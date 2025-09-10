-- เพิ่มคอลัมน์ ResultDetail ในตาราง Evaluation
ALTER TABLE Evaluation
ADD ResultDetail NVARCHAR(500) NULL;

-- คำสั่งสำรอง (หากต้องการลบคอลัมน์)
-- ALTER TABLE Evaluation DROP COLUMN ResultDetail;
