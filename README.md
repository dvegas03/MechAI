# MechAI

An XR assistant for car maintenance. Point the headset at an open hood: a YOLO model detects the parts, the app highlights them in place and suggests repairs.

Built for CS 116A Introduction to Computer Graphics, San Jose State University, Fall 2025, by a team of three. My part: training the YOLO detector and building the Unity XR app.

## What is in the repo

- `Unity/` - the XR app: YOLO inference on the Unity Inference Engine (ONNX, 640x640 input), detection logic with non-max suppression, part highlighting and repair suggestions, plus unit and integration tests for the detector.
- `ML/` - dataset instructions for training the YOLO detector.
- `backend/` - the database and FastAPI backend.
- `frontend/` - the web frontend.
- `diagrams/` - architecture diagrams.

## Stack

Python, YOLO, ONNX; Unity (C#), Unity Inference Engine.

## Dataset

Car parts detection dataset from Kaggle; see the link in `ML/`.

## Team

- Danila Kharitonenkov ([dvegas03](https://github.com/dvegas03)) - YOLO training, Unity XR app
- Alex Hovakimyan ([hovak101](https://github.com/hovak101)) - database and backend API
- Anhadh Singh Sran ([anhadh3101](https://github.com/anhadh3101)) - web frontend
