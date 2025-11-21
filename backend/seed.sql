USE mech_ai_db;

DROP TABLE IF EXISTS instructions;

CREATE TABLE instructions (
    ID INT AUTO_INCREMENT PRIMARY KEY,
    StepNum INT NOT NULL,
    InstructionText VARCHAR(255)
);

INSERT INTO instructions (StepNum, InstructionText) VALUES 
(1, 'Check the hydraulic pressure levels.'),
(2, 'Calibrate the sensor array.'),
(3, 'Initiate the startup sequence.');