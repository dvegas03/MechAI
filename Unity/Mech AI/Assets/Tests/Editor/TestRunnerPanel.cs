using UnityEditor;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class TestRunnerPanel : EditorWindow
{
    private TestRunnerApi _testRunnerApi;
    private Vector2 _scrollPosition;
    private List<TestFailureInfo> _failedTests = new List<TestFailureInfo>();
    private List<string> _passedTests = new List<string>();
    private int _passCount;
    private int _failCount;
    private int _skipCount;
    private bool _testsRunning;
    private double _totalDuration;
    private Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();

    private class TestFailureInfo
    {
        public string Name;
        public string FullName;
        public string Message;
        public string StackTrace;
        public string Output;
        public double Duration;
        public string ResultState;
    }

    [MenuItem("Window/Test Runner Panel")]
    public static void ShowWindow()
    {
        GetWindow<TestRunnerPanel>("Test Runner");
    }

    private void OnEnable()
    {
        _testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        _testRunnerApi.RegisterCallbacks(new TestCallbacks(this));
    }

    private void OnDisable()
    {
        _testRunnerApi.UnregisterCallbacks(new TestCallbacks(this));
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Test Runner", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(_testsRunning);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Run All Tests"))
        {
            RunTests(new Filter { testMode = TestMode.PlayMode }, new Filter { testMode = TestMode.EditMode });
        }
        if (GUILayout.Button("Run Unit (Edit)"))
        {
            RunTests(new Filter { testMode = TestMode.EditMode });
        }
        if (GUILayout.Button("Run Integration (Play)"))
        {
            RunTests(new Filter { testMode = TestMode.PlayMode });
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();

        if (_testsRunning)
        {
            EditorGUILayout.HelpBox("Tests are running...", MessageType.Info);
        }

        EditorGUILayout.Space();
        
        // Summary
        var summaryStyle = new GUIStyle(EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Results: {_passCount} Passed, {_failCount} Failed, {_skipCount} Skipped ({_totalDuration:F2}s)", summaryStyle);

        if (_failCount > 0)
        {
            EditorGUILayout.HelpBox($"{_failCount} test(s) failed! See details below and check Console for full logs.", MessageType.Error);
        }
        else if (_passCount > 0 && !_testsRunning)
        {
            EditorGUILayout.HelpBox("All tests passed!", MessageType.Info);
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // Failed Tests Section
        if (_failedTests.Any())
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Failed Tests:", EditorStyles.boldLabel);
            
            foreach (var failure in _failedTests)
            {
                DrawFailedTest(failure);
            }
        }

        // Passed Tests Section (collapsible)
        if (_passedTests.Any())
        {
            EditorGUILayout.Space();
            if (!_foldouts.ContainsKey("passed")) _foldouts["passed"] = false;
            _foldouts["passed"] = EditorGUILayout.Foldout(_foldouts["passed"], $"Passed Tests ({_passedTests.Count})", true);
            
            if (_foldouts["passed"])
            {
                EditorGUI.indentLevel++;
                foreach (var test in _passedTests)
                {
                    EditorGUILayout.LabelField($"✓ {test}", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawFailedTest(TestFailureInfo failure)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Header with foldout
        if (!_foldouts.ContainsKey(failure.FullName)) _foldouts[failure.FullName] = true;
        
        EditorGUILayout.BeginHorizontal();
        var headerStyle = new GUIStyle(EditorStyles.foldout) { richText = true };
        _foldouts[failure.FullName] = EditorGUILayout.Foldout(_foldouts[failure.FullName], $"<color=red>✗</color> {failure.Name} ({failure.Duration:F3}s)", true, headerStyle);
        
        if (GUILayout.Button("Log to Console", GUILayout.Width(110)))
        {
            LogFailureToConsole(failure);
        }
        EditorGUILayout.EndHorizontal();

        if (_foldouts[failure.FullName])
        {
            EditorGUI.indentLevel++;
            
            // Full name
            EditorGUILayout.LabelField("Full Name:", EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(failure.FullName, EditorStyles.miniLabel, GUILayout.Height(18));
            
            // Result state
            EditorGUILayout.LabelField($"Result: {failure.ResultState}", EditorStyles.miniLabel);
            
            // Error message
            if (!string.IsNullOrEmpty(failure.Message))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Error Message:", EditorStyles.miniBoldLabel);
                var messageStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                EditorGUILayout.SelectableLabel(failure.Message, messageStyle, GUILayout.MinHeight(40));
            }
            
            // Stack trace
            if (!string.IsNullOrEmpty(failure.StackTrace))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Stack Trace:", EditorStyles.miniBoldLabel);
                var stackStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true, fontSize = 10 };
                EditorGUILayout.SelectableLabel(failure.StackTrace, stackStyle, GUILayout.MinHeight(60));
            }
            
            // Test output
            if (!string.IsNullOrEmpty(failure.Output))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Test Output:", EditorStyles.miniBoldLabel);
                var outputStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                EditorGUILayout.SelectableLabel(failure.Output, outputStyle, GUILayout.MinHeight(40));
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
    }

    private void LogFailureToConsole(TestFailureInfo failure)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"TEST FAILURE: {failure.Name}");
        sb.AppendLine($"═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"Full Name: {failure.FullName}");
        sb.AppendLine($"Result: {failure.ResultState}");
        sb.AppendLine($"Duration: {failure.Duration:F3}s");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(failure.Message))
        {
            sb.AppendLine("─── ERROR MESSAGE ───");
            sb.AppendLine(failure.Message);
            sb.AppendLine();
        }
        
        if (!string.IsNullOrEmpty(failure.StackTrace))
        {
            sb.AppendLine("─── STACK TRACE ───");
            sb.AppendLine(failure.StackTrace);
            sb.AppendLine();
        }
        
        if (!string.IsNullOrEmpty(failure.Output))
        {
            sb.AppendLine("─── TEST OUTPUT ───");
            sb.AppendLine(failure.Output);
            sb.AppendLine();
        }
        
        sb.AppendLine($"═══════════════════════════════════════════════════════════════");
        
        Debug.LogError(sb.ToString());
    }

    private void RunTests(params Filter[] filters)
    {
        _failedTests.Clear();
        _passedTests.Clear();
        _foldouts.Clear();
        _passCount = 0;
        _failCount = 0;
        _skipCount = 0;
        _totalDuration = 0;
        _testsRunning = true;
        
        Debug.Log("═══════════════════════════════════════════════════════════════");
        Debug.Log("STARTING TEST RUN...");
        Debug.Log("═══════════════════════════════════════════════════════════════");
        
        _testRunnerApi.Execute(new ExecutionSettings(filters));
    }

    private class TestCallbacks : ICallbacks
    {
        private readonly TestRunnerPanel _panel;
        public TestCallbacks(TestRunnerPanel panel) { _panel = panel; }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            _panel._testsRunning = true;
            Debug.Log($"Running {CountTests(testsToRun)} test(s)...");
            _panel.Repaint();
        }

        private int CountTests(ITestAdaptor test)
        {
            if (!test.IsSuite) return 1;
            int count = 0;
            foreach (var child in test.Children)
                count += CountTests(child);
            return count;
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            _panel._testsRunning = false;
            
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("TEST RUN COMPLETE");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  Passed:  {_panel._passCount}");
            sb.AppendLine($"  Failed:  {_panel._failCount}");
            sb.AppendLine($"  Skipped: {_panel._skipCount}");
            sb.AppendLine($"  Total:   {_panel._passCount + _panel._failCount + _panel._skipCount}");
            sb.AppendLine($"  Duration: {_panel._totalDuration:F2}s");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            
            if (_panel._failCount > 0)
            {
                Debug.LogError(sb.ToString());
            }
            else
            {
                Debug.Log(sb.ToString());
            }
            
            _panel.Repaint();
        }

        public void TestStarted(ITestAdaptor test)
        {
            if (!test.IsSuite)
            {
                Debug.Log($"  → Running: {test.Name}");
            }
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.IsSuite) return;

            _panel._totalDuration += result.Duration;

            if (result.TestStatus == TestStatus.Passed)
            {
                _panel._passCount++;
                _panel._passedTests.Add(result.Test.Name);
                Debug.Log($"  ✓ PASSED: {result.Test.Name} ({result.Duration:F3}s)");
            }
            else if (result.TestStatus == TestStatus.Failed)
            {
                _panel._failCount++;
                
                var failureInfo = new TestFailureInfo
                {
                    Name = result.Test.Name,
                    FullName = result.Test.FullName,
                    Message = result.Message,
                    StackTrace = result.StackTrace,
                    Output = result.Output,
                    Duration = result.Duration,
                    ResultState = result.ResultState
                };
                _panel._failedTests.Add(failureInfo);
                
                // Log to console immediately
                var sb = new StringBuilder();
                sb.AppendLine($"  ✗ FAILED: {result.Test.Name} ({result.Duration:F3}s)");
                if (!string.IsNullOrEmpty(result.Message))
                {
                    sb.AppendLine($"    Message: {result.Message}");
                }
                if (!string.IsNullOrEmpty(result.StackTrace))
                {
                    sb.AppendLine($"    Stack Trace:");
                    foreach (var line in result.StackTrace.Split('\n').Take(5))
                    {
                        sb.AppendLine($"      {line.Trim()}");
                    }
                    if (result.StackTrace.Split('\n').Length > 5)
                    {
                        sb.AppendLine($"      ... (see panel for full trace)");
                    }
                }
                if (!string.IsNullOrEmpty(result.Output))
                {
                    sb.AppendLine($"    Output: {result.Output}");
                }
                
                Debug.LogError(sb.ToString());
            }
            else if (result.TestStatus == TestStatus.Skipped)
            {
                _panel._skipCount++;
                Debug.LogWarning($"  ⊘ SKIPPED: {result.Test.Name} - {result.Message}");
            }
            else
            {
                // Inconclusive or other states
                _panel._skipCount++;
                Debug.LogWarning($"  ? {result.ResultState}: {result.Test.Name}");
            }
            
            _panel.Repaint();
        }
    }
}
