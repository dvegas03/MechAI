-- MechAI Database Schema and Seed Data
-- This schema supports a mechanic AI system with car models, maintenance lessons, and instructional steps

USE mech_ai_db;

-- Drop existing tables in correct order (respecting foreign key constraints)
DROP TABLE IF EXISTS Lesson_Instructions;
DROP TABLE IF EXISTS Car_Lessons;
DROP TABLE IF EXISTS Instructions;
DROP TABLE IF EXISTS Lessons;
DROP TABLE IF EXISTS Car;
DROP TABLE IF EXISTS Make;

-- ============================================
-- TABLE: Make (Car Manufacturers)
-- ============================================
CREATE TABLE Make (
    MakeID INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    INDEX idx_make_name (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- TABLE: Car (Car Models)
-- ============================================
CREATE TABLE Car (
    ModelID INT AUTO_INCREMENT PRIMARY KEY,
    MakeID INT NOT NULL,
    ModelName VARCHAR(100) NOT NULL,
    Year INT NOT NULL,
    FOREIGN KEY (MakeID) REFERENCES Make(MakeID) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_car_make (MakeID),
    INDEX idx_car_year (Year),
    CONSTRAINT chk_year CHECK (Year >= 1900 AND Year <= 2030)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- TABLE: Lessons (Maintenance/Repair Topics)
-- ============================================
CREATE TABLE Lessons (
    LessonID INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(200) NOT NULL UNIQUE,
    INDEX idx_lesson_name (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- TABLE: Instructions (Reusable Instruction Steps)
-- ============================================
CREATE TABLE Instructions (
    InstructionID INT AUTO_INCREMENT PRIMARY KEY,
    Text TEXT NOT NULL,
    YOLOClasses VARCHAR(500),
    INDEX idx_instruction_yolo (YOLOClasses(100))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- TABLE: Car_Lessons (Many-to-Many Junction)
-- Links car models to applicable lessons
-- ============================================
CREATE TABLE Car_Lessons (
    ModelID INT NOT NULL,
    LessonID INT NOT NULL,
    PRIMARY KEY (ModelID, LessonID),
    FOREIGN KEY (ModelID) REFERENCES Car(ModelID) ON DELETE CASCADE ON UPDATE CASCADE,
    FOREIGN KEY (LessonID) REFERENCES Lessons(LessonID) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_car_lessons_lesson (LessonID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- TABLE: Lesson_Instructions (Many-to-Many Junction with Ordering)
-- Links lessons to their instructions in sequential order
-- ============================================
CREATE TABLE Lesson_Instructions (
    LessonID INT NOT NULL,
    InstructionID INT NOT NULL,
    StepNum INT NOT NULL,
    PRIMARY KEY (LessonID, InstructionID),
    UNIQUE KEY uk_lesson_step (LessonID, StepNum),
    FOREIGN KEY (LessonID) REFERENCES Lessons(LessonID) ON DELETE CASCADE ON UPDATE CASCADE,
    FOREIGN KEY (InstructionID) REFERENCES Instructions(InstructionID) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_lesson_instructions_instruction (InstructionID),
    INDEX idx_lesson_instructions_step (StepNum),
    CONSTRAINT chk_stepnum CHECK (StepNum > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- SEED DATA: Make (Car Manufacturers)
-- ============================================
INSERT INTO Make (MakeID, Name) VALUES
(1, 'Toyota'),
(2, 'Ford'),
(3, 'Honda'),
(4, 'Chevrolet'),
(5, 'BMW'),
(6, 'Mercedes-Benz'),
(7, 'Volkswagen'),
(8, 'Nissan'),
(9, 'Hyundai'),
(10, 'Mazda');

-- ============================================
-- SEED DATA: Car (Car Models)
-- ============================================
INSERT INTO Car (ModelID, MakeID, ModelName, Year) VALUES
-- Toyota models
(1, 1, 'Camry', 2020),
(2, 1, 'Corolla', 2021),
(3, 1, 'RAV4', 2022),
-- Ford models
(4, 2, 'F-150', 2021),
(5, 2, 'Mustang', 2020),
(6, 2, 'Explorer', 2023),
-- Honda models
(7, 3, 'Civic', 2022),
(8, 3, 'Accord', 2021),
(9, 3, 'CR-V', 2023),
-- Chevrolet models
(10, 4, 'Silverado', 2020),
(11, 4, 'Malibu', 2021),
(12, 4, 'Equinox', 2022),
-- BMW models
(13, 5, '3 Series', 2023),
(14, 5, 'X5', 2022),
-- Mercedes-Benz models
(15, 6, 'C-Class', 2021),
(16, 6, 'E-Class', 2023),
-- Volkswagen models
(17, 7, 'Golf', 2022),
(18, 7, 'Jetta', 2021),
-- Nissan models
(19, 8, 'Altima', 2020),
(20, 8, 'Rogue', 2022),
-- Hyundai models
(21, 9, 'Elantra', 2023),
(22, 9, 'Tucson', 2022),
-- Mazda models
(23, 10, 'Mazda3', 2021),
(24, 10, 'CX-5', 2023);

-- ============================================
-- SEED DATA: Lessons (Maintenance Topics)
-- ============================================
INSERT INTO Lessons (LessonID, Name) VALUES
(1, 'Oil Change'),
(2, 'Brake Pad Replacement'),
(3, 'Air Filter Replacement'),
(4, 'Spark Plug Replacement'),
(5, 'Tire Rotation'),
(6, 'Battery Replacement'),
(7, 'Coolant Flush'),
(8, 'Transmission Fluid Change'),
(9, 'Windshield Wiper Replacement'),
(10, 'Headlight Bulb Replacement'),
(11, 'Cabin Air Filter Replacement'),
(12, 'Serpentine Belt Replacement'),
(13, 'Brake Fluid Flush'),
(14, 'Power Steering Fluid Check'),
(15, 'Tire Pressure Check');

-- ============================================
-- SEED DATA: Instructions (Reusable Steps)
-- ============================================
INSERT INTO Instructions (InstructionID, Text, YOLOClasses) VALUES
-- Oil Change Instructions (IDs 1-14)
(1, 'Locate the oil drain plug underneath the vehicle', 'drain_plug,vehicle_underside,wrench'),
(2, 'Place an oil pan underneath the drain plug', 'oil_pan,drain_plug'),
(3, 'Use a wrench to loosen and remove the drain plug', 'wrench,drain_plug,hand'),
(4, 'Allow oil to completely drain into the pan (5-10 minutes)', 'oil_pan,oil_stream'),
(5, 'Replace the drain plug and tighten securely', 'drain_plug,wrench,hand'),
(6, 'Locate the oil filter near the engine block', 'oil_filter,engine_block'),
(7, 'Remove the old oil filter using an oil filter wrench', 'oil_filter,oil_filter_wrench,hand'),
(8, 'Apply a thin layer of new oil to the new filter gasket', 'oil_filter,oil_bottle,hand'),
(9, 'Install the new oil filter by hand, tightening firmly', 'oil_filter,hand'),
(10, 'Open the hood and locate the oil fill cap', 'hood,oil_fill_cap,engine'),
(11, 'Add the recommended amount of new oil', 'oil_bottle,funnel,oil_fill_cap'),
(12, 'Replace the oil fill cap and close the hood', 'oil_fill_cap,hood'),
(13, 'Start the engine and check for leaks', 'engine,vehicle_underside'),
(14, 'Turn off the engine and check the oil level with dipstick', 'dipstick,hand'),

-- Brake Pad Replacement Instructions (IDs 15-32)
(15, 'Park on a level surface and engage parking brake', 'vehicle,parking_brake'),
(16, 'Loosen the lug nuts on the wheel (do not remove yet)', 'lug_wrench,wheel,lug_nuts'),
(17, 'Jack up the vehicle to the appropriate height', 'jack,jack_point,vehicle'),
(18, 'Place jack stands under the vehicle for safety', 'jack_stand,vehicle_frame'),
(19, 'Remove the lug nuts and take off the wheel', 'lug_wrench,wheel,lug_nuts,hand'),
(20, 'Locate the brake caliper and identify the mounting bolts', 'brake_caliper,caliper_bolts,brake_rotor'),
(21, 'Remove the brake caliper bolts using appropriate socket', 'socket_wrench,caliper_bolts'),
(22, 'Carefully lift the caliper off the brake rotor', 'brake_caliper,brake_rotor,hand'),
(23, 'Support the caliper with wire or bungee cord', 'brake_caliper,wire_hanger'),
(24, 'Remove the old brake pads from the caliper bracket', 'brake_pads,caliper_bracket,hand'),
(25, 'Compress the caliper piston using a C-clamp or piston tool', 'c_clamp,caliper_piston,brake_caliper'),
(26, 'Install the new brake pads into the caliper bracket', 'brake_pads,caliper_bracket,hand'),
(27, 'Reposition the caliper over the new brake pads', 'brake_caliper,brake_pads'),
(28, 'Reinstall and tighten the caliper bolts to specification', 'socket_wrench,caliper_bolts'),
(29, 'Reinstall the wheel and hand-tighten the lug nuts', 'wheel,lug_nuts,hand'),
(30, 'Lower the vehicle and tighten lug nuts in star pattern', 'lug_wrench,wheel,vehicle'),
(31, 'Pump the brake pedal several times to seat the pads', 'brake_pedal,foot'),
(32, 'Test drive at low speed and test brakes', 'vehicle,steering_wheel,brake_pedal'),

-- Air Filter Replacement Instructions (IDs 33-41)
(33, 'Open the hood and locate the air filter housing', 'hood,air_filter_housing,engine'),
(34, 'Release the clips or remove screws securing the housing cover', 'screwdriver,clips,air_filter_housing'),
(35, 'Lift the housing cover to access the air filter', 'air_filter_housing,hand'),
(36, 'Remove the old air filter from the housing', 'air_filter,air_filter_housing,hand'),
(37, 'Clean any debris from inside the housing', 'rag,air_filter_housing'),
(38, 'Insert the new air filter into the housing', 'air_filter,air_filter_housing,hand'),
(39, 'Ensure the filter is properly seated', 'air_filter,air_filter_housing'),
(40, 'Replace the housing cover and secure clips/screws', 'air_filter_housing,clips,screwdriver'),
(41, 'Close the hood', 'hood,hand'),

-- Spark Plug Replacement Instructions (IDs 42-52)
(42, 'Open the hood and locate the spark plug wires or coil packs', 'hood,spark_plug_wires,ignition_coils,engine'),
(43, 'Remove the first spark plug wire or coil pack', 'spark_plug_wire,ignition_coil,hand'),
(44, 'Use compressed air to clean around the spark plug', 'compressed_air,spark_plug_well'),
(45, 'Use a spark plug socket to remove the old spark plug', 'spark_plug_socket,ratchet,spark_plug'),
(46, 'Check the spark plug gap on the new plug', 'spark_plug,gap_tool,hand'),
(47, 'Apply anti-seize compound to the plug threads (if recommended)', 'spark_plug,anti_seize,hand'),
(48, 'Hand-thread the new spark plug into the cylinder head', 'spark_plug,hand'),
(49, 'Tighten the spark plug to specified torque', 'spark_plug_socket,torque_wrench'),
(50, 'Reconnect the spark plug wire or coil pack', 'spark_plug_wire,ignition_coil,hand'),
(51, 'Repeat for all remaining spark plugs', 'spark_plugs,engine'),
(52, 'Close the hood and start the engine', 'hood,ignition_key'),

-- Battery Replacement Instructions (IDs 53-66)
(53, 'Turn off the engine and remove the key', 'ignition_key,hand'),
(54, 'Open the hood and locate the battery', 'hood,battery,engine'),
(55, 'Identify positive and negative battery terminals', 'battery,positive_terminal,negative_terminal'),
(56, 'Disconnect the negative terminal first using a wrench', 'wrench,negative_terminal,battery'),
(57, 'Disconnect the positive terminal', 'wrench,positive_terminal,battery'),
(58, 'Remove the battery hold-down bracket or clamp', 'wrench,battery_clamp,battery'),
(59, 'Carefully lift the old battery out of the vehicle', 'battery,hand'),
(60, 'Clean the battery tray and terminals', 'wire_brush,battery_tray,terminals'),
(61, 'Place the new battery in the tray', 'battery,battery_tray,hand'),
(62, 'Install the battery hold-down bracket', 'battery_clamp,wrench,battery'),
(63, 'Connect the positive terminal first and tighten', 'wrench,positive_terminal,battery'),
(64, 'Connect the negative terminal and tighten', 'wrench,negative_terminal,battery'),
(65, 'Apply terminal protector spray to prevent corrosion', 'terminal_protector,battery_terminals'),
(66, 'Close the hood and test the vehicle', 'hood,ignition_key');

-- ============================================
-- SEED DATA: Car_Lessons (Which lessons apply to which cars)
-- ============================================
INSERT INTO Car_Lessons (ModelID, LessonID) VALUES
-- Toyota Camry 2020 (ModelID 1)
(1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6), (1, 9), (1, 10), (1, 15),
-- Toyota Corolla 2021 (ModelID 2)
(2, 1), (2, 2), (2, 3), (2, 4), (2, 5), (2, 6), (2, 9), (2, 10), (2, 15),
-- Toyota RAV4 2022 (ModelID 3)
(3, 1), (3, 2), (3, 3), (3, 4), (3, 5), (3, 6), (3, 9), (3, 10), (3, 15),
-- Ford F-150 2021 (ModelID 4)
(4, 1), (4, 2), (4, 3), (4, 4), (4, 5), (4, 6), (4, 8), (4, 9), (4, 10), (4, 15),
-- Ford Mustang 2020 (ModelID 5)
(5, 1), (5, 2), (5, 3), (5, 4), (5, 5), (5, 6), (5, 9), (5, 10), (5, 15),
-- Ford Explorer 2023 (ModelID 6)
(6, 1), (6, 2), (6, 3), (6, 4), (6, 5), (6, 6), (6, 9), (6, 10), (6, 15),
-- Honda Civic 2022 (ModelID 7)
(7, 1), (7, 2), (7, 3), (7, 4), (7, 5), (7, 6), (7, 9), (7, 10), (7, 11), (7, 15),
-- Honda Accord 2021 (ModelID 8)
(8, 1), (8, 2), (8, 3), (8, 4), (8, 5), (8, 6), (8, 9), (8, 10), (8, 11), (8, 15),
-- Honda CR-V 2023 (ModelID 9)
(9, 1), (9, 2), (9, 3), (9, 4), (9, 5), (9, 6), (9, 9), (9, 10), (9, 11), (9, 15),
-- Chevrolet Silverado 2020 (ModelID 10)
(10, 1), (10, 2), (10, 3), (10, 4), (10, 5), (10, 6), (10, 8), (10, 9), (10, 10), (10, 15),
-- Chevrolet Malibu 2021 (ModelID 11)
(11, 1), (11, 2), (11, 3), (11, 4), (11, 5), (11, 6), (11, 9), (11, 10), (11, 15),
-- Chevrolet Equinox 2022 (ModelID 12)
(12, 1), (12, 2), (12, 3), (12, 4), (12, 5), (12, 6), (12, 9), (12, 10), (12, 15),
-- BMW 3 Series 2023 (ModelID 13)
(13, 1), (13, 2), (13, 3), (13, 4), (13, 5), (13, 6), (13, 9), (13, 10), (13, 12), (13, 15),
-- BMW X5 2022 (ModelID 14)
(14, 1), (14, 2), (14, 3), (14, 4), (14, 5), (14, 6), (14, 9), (14, 10), (14, 12), (14, 15),
-- Mercedes-Benz C-Class 2021 (ModelID 15)
(15, 1), (15, 2), (15, 3), (15, 4), (15, 5), (15, 6), (15, 9), (15, 10), (15, 12), (15, 15),
-- Mercedes-Benz E-Class 2023 (ModelID 16)
(16, 1), (16, 2), (16, 3), (16, 4), (16, 5), (16, 6), (16, 9), (16, 10), (16, 12), (16, 15),
-- Volkswagen Golf 2022 (ModelID 17)
(17, 1), (17, 2), (17, 3), (17, 4), (17, 5), (17, 6), (17, 9), (17, 10), (17, 15),
-- Volkswagen Jetta 2021 (ModelID 18)
(18, 1), (18, 2), (18, 3), (18, 4), (18, 5), (18, 6), (18, 9), (18, 10), (18, 15),
-- Nissan Altima 2020 (ModelID 19)
(19, 1), (19, 2), (19, 3), (19, 4), (19, 5), (19, 6), (19, 9), (19, 10), (19, 15),
-- Nissan Rogue 2022 (ModelID 20)
(20, 1), (20, 2), (20, 3), (20, 4), (20, 5), (20, 6), (20, 9), (20, 10), (20, 15),
-- Hyundai Elantra 2023 (ModelID 21)
(21, 1), (21, 2), (21, 3), (21, 4), (21, 5), (21, 6), (21, 9), (21, 10), (21, 15),
-- Hyundai Tucson 2022 (ModelID 22)
(22, 1), (22, 2), (22, 3), (22, 4), (22, 5), (22, 6), (22, 9), (22, 10), (22, 15),
-- Mazda Mazda3 2021 (ModelID 23)
(23, 1), (23, 2), (23, 3), (23, 4), (23, 5), (23, 6), (23, 9), (23, 10), (23, 15),
-- Mazda CX-5 2023 (ModelID 24)
(24, 1), (24, 2), (24, 3), (24, 4), (24, 5), (24, 6), (24, 9), (24, 10), (24, 15);

-- ============================================
-- SEED DATA: Lesson_Instructions (Ordered steps for each lesson)
-- ============================================

-- Oil Change Lesson (LessonID 1)
INSERT INTO Lesson_Instructions (LessonID, InstructionID, StepNum) VALUES
(1, 1, 1), (1, 2, 2), (1, 3, 3), (1, 4, 4), (1, 5, 5),
(1, 6, 6), (1, 7, 7), (1, 8, 8), (1, 9, 9), (1, 10, 10),
(1, 11, 11), (1, 12, 12), (1, 13, 13), (1, 14, 14);

-- Brake Pad Replacement Lesson (LessonID 2)
INSERT INTO Lesson_Instructions (LessonID, InstructionID, StepNum) VALUES
(2, 15, 1), (2, 16, 2), (2, 17, 3), (2, 18, 4), (2, 19, 5),
(2, 20, 6), (2, 21, 7), (2, 22, 8), (2, 23, 9), (2, 24, 10),
(2, 25, 11), (2, 26, 12), (2, 27, 13), (2, 28, 14), (2, 29, 15),
(2, 30, 16), (2, 31, 17), (2, 32, 18);

-- Air Filter Replacement Lesson (LessonID 3)
INSERT INTO Lesson_Instructions (LessonID, InstructionID, StepNum) VALUES
(3, 33, 1), (3, 34, 2), (3, 35, 3), (3, 36, 4), (3, 37, 5),
(3, 38, 6), (3, 39, 7), (3, 40, 8), (3, 41, 9);

-- Spark Plug Replacement Lesson (LessonID 4)
INSERT INTO Lesson_Instructions (LessonID, InstructionID, StepNum) VALUES
(4, 42, 1), (4, 43, 2), (4, 44, 3), (4, 45, 4), (4, 46, 5),
(4, 47, 6), (4, 48, 7), (4, 49, 8), (4, 50, 9), (4, 51, 10),
(4, 52, 11);

-- Battery Replacement Lesson (LessonID 6)
INSERT INTO Lesson_Instructions (LessonID, InstructionID, StepNum) VALUES
(6, 53, 1), (6, 54, 2), (6, 55, 3), (6, 56, 4), (6, 57, 5),
(6, 58, 6), (6, 59, 7), (6, 60, 8), (6, 61, 9), (6, 62, 10),
(6, 63, 11), (6, 64, 12), (6, 65, 13), (6, 66, 14);

-- ============================================
-- DATABASE SCHEMA COMPLETE
-- ============================================
