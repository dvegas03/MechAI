import React, { useState, useEffect } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';

// API Configuration
const API_BASE_URL = 'http://localhost:80'; // Adjust if your backend runs on a different port

// Types
interface StepDetail {
  id: string;
  title: string;
  body: string;
  yolo_class: string | null;
}

interface ProcedureData {
  issue: string;
  steps: StepDetail[];
}

// Fallback data before Unity sends real data
const initialData: ProcedureData = {
  issue: 'Fetching procedure from AI...',
  steps: [
    {
      id: '',
      title: 'Waiting...',
      body: 'Connect to car and analyze engine state',
      yolo_class: null
    }
  ]
};

export default function App() {
  const [data, setData] = useState<ProcedureData>(initialData);
  const [currentStep, setCurrentStep] = useState(0);
  const [loadingSteps, setLoadingSteps] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Fetch step details from API
  const fetchStepDetails = async (stepId: string): Promise<StepDetail | null> => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/steps/${stepId}`);
      
      if (!response.ok) {
        throw new Error(`Failed to fetch step ${stepId}: ${response.statusText}`);
      }
      
      const stepData: StepDetail = await response.json();
      return stepData;
    } catch (err) {
      console.error(`Error fetching step ${stepId}:`, err);
      return null;
    }
  };

  // Fetch all step details from API
  const fetchAllStepDetails = async (stepIds: string[]): Promise<StepDetail[]> => {
    const stepPromises = stepIds.map(stepId => fetchStepDetails(stepId));
    const results = await Promise.all(stepPromises);
    // Filter out any null results (failed fetches)
    return results.filter((step): step is StepDetail => step !== null);
  };

  // Expose a global function Unity can call
  useEffect(() => {
    // Use window.setProcedureFromUnity({
    //   issue: '...',
    //   stepIds: ['step_1', 'step_2', 'step_3']
    // });
    // in Unity, to push data to the frontend web app.
    (window as any).setProcedureFromUnity = async (incoming: { issue: string; stepIds: string[] }) => {
      if (!incoming || !Array.isArray(incoming.stepIds) || incoming.stepIds.length === 0) {
        return;
      }

      setLoadingSteps(true);
      setError(null);
      setCurrentStep(0);

      try {
        // Fetch all step details from the API
        const stepDetails = await fetchAllStepDetails(incoming.stepIds);
        
        if (stepDetails.length === 0) {
          setError('Failed to load step details');
          setLoadingSteps(false);
          return;
        }

        setData({
          issue: incoming.issue,
          steps: stepDetails
        });
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load steps');
        console.error('Error loading steps:', err);
      } finally {
        setLoadingSteps(false);
      }
    };

    return () => {
      delete (window as any).setProcedureFromUnity;
    };
  }, []);

  const totalSteps = data.steps.length;

  const handleNext = () => {
    if (currentStep < totalSteps - 1) {
      setCurrentStep(currentStep + 1);
    }
  };

  const handlePrevious = () => {
    if (currentStep > 0) {
      setCurrentStep(currentStep - 1);
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center p-6">
      <div className="w-full max-w-2xl bg-white rounded-lg shadow-lg p-8">
        {/* Issue Title */}
        <h1 className="text-3xl text-gray-900 mb-2">{data.issue}</h1>
        <p className="text-gray-600 mb-8">
          {totalSteps > 0
            ? `Step ${currentStep + 1} of ${totalSteps}`
            : 'No steps available'}
        </p>

        {/* Current Step */}
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-6 mb-8 min-h-32">
          {loadingSteps ? (
            <div className="text-gray-600">Loading step details...</div>
          ) : error ? (
            <div className="text-red-600">{error}</div>
          ) : totalSteps > 0 ? (
            <>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">
                {data.steps[currentStep].title}
              </h2>
              <p className="text-lg text-gray-800">
                {data.steps[currentStep].body}
              </p>
            </>
          ) : (
            <p className="text-xl text-gray-800">Waiting for steps...</p>
          )}
        </div>

        {/* Navigation Buttons */}
        <div className="flex gap-4">
          <button
            onClick={handlePrevious}
            disabled={currentStep === 0 || totalSteps === 0}
            className="flex items-center gap-2 px-6 py-3 bg-gray-200 text-gray-800 rounded-lg hover:bg-gray-300 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <ChevronLeft className="w-5 h-5" />
            Previous
          </button>
          
          <button
            onClick={handleNext}
            disabled={currentStep === totalSteps - 1 || totalSteps === 0}
            className="flex-1 flex items-center justify-center gap-2 px-6 py-3 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Next
            <ChevronRight className="w-5 h-5" />
          </button>
        </div>
      </div>
    </div>
  );
}