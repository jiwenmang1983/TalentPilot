-- T-53: 匹配引擎8维度扩展
-- MySQL talentpilot database

-- 1. MatchResults 表新增维度分数字段
ALTER TABLE MatchResults
  ADD COLUMN DimensionScores TEXT NULL COMMENT '8维度JSON: skillScore/experienceScore/educationScore/industryScore/levelScore/salaryScore/locationScore/turnoverScore',
  ADD COLUMN DimensionWeights TEXT NULL COMMENT '权重JSON: skillWeight/experienceWeight/educationWeight/industryWeight/levelWeight/salaryWeight/locationWeight/turnoverWeight',
  ADD COLUMN MatchThreshold DECIMAL(5,2) NULL COMMENT '该匹配的阈值(继承职位默认值)';

-- 2. JobPosts 表新增匹配阈值和权重字段
ALTER TABLE JobPosts
  ADD COLUMN MatchThreshold DECIMAL(5,2) NOT NULL DEFAULT 80.00 COMMENT '职位默认匹配阈值(0-100)',
  ADD COLUMN MatchWeights TEXT NULL COMMENT '职位自定义权重JSON(如不设则用系统默认)';

-- 3. 验证
-- SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_COMMENT FROM INFORMATION_SCHEMA.COLUMNS
--   WHERE TABLE_NAME = 'MatchResults' AND COLUMN_NAME IN ('DimensionScores','DimensionWeights','MatchThreshold');

-- 4. 默认权重值（系统默认，不受职位自定义影响时使用）
-- 系统默认: 技能30%/年限20%/学历15%/行业10%/级别10%/薪资5%/地理5%/跳槽5%
-- 存储在 JobPosts.MatchWeights，格式:
-- {"skillWeight":30,"experienceWeight":20,"educationWeight":15,"industryWeight":10,"levelWeight":10,"salaryWeight":5,"locationWeight":5,"turnoverWeight":5}

-- 5. 新增 API 端点验证:
-- PUT /api/jobposts/{id}/match-threshold  (更新 MatchThreshold)
-- PUT /api/jobposts/{id}/match-weights    (更新 MatchWeights)