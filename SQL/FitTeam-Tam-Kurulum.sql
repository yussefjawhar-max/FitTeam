-- FitTeam tam kurulum şeması / MySQL 8.4
-- Gerçek kullanıcı verisi veya bağlantı parolası içermez.
CREATE DATABASE IF NOT EXISTS fitteam CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_ci;
USE fitteam;
SET NAMES utf8mb4;

-- MySQL 8.4 / utf8mb4. Bu dosya uygulamanın seçili veritabanında çalıştırılır.
CREATE TABLE IF NOT EXISTS app_state (
  id TINYINT PRIMARY KEY,
  revision BIGINT NOT NULL DEFAULT 0,
  metadata JSON NOT NULL
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS users (
  id CHAR(36) PRIMARY KEY,
  username VARCHAR(80) NOT NULL,
  username_key VARBINARY(320) NOT NULL UNIQUE,
  full_name VARCHAR(200) NOT NULL,
  role VARCHAR(80) NOT NULL,
  email VARCHAR(254) NULL,
  email_key VARBINARY(1020) NULL UNIQUE,
  phone VARCHAR(32) NOT NULL,
  active BOOLEAN NOT NULL,
  needs_approval BOOLEAN NOT NULL,
  password_hash VARCHAR(256) NOT NULL,
  salt VARCHAR(128) NOT NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_users_role (role), INDEX ix_users_phone (phone)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS profiles (
  user_id CHAR(36) PRIMARY KEY,
  age INT NOT NULL,
  height_cm DECIMAL(8,2) NOT NULL,
  weight_kg DECIMAL(8,2) NOT NULL,
  target_weight_kg DECIMAL(8,2) NOT NULL,
  goal VARCHAR(100) NOT NULL,
  payload JSON NOT NULL,
  CONSTRAINT fk_profile_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS member_coaches (
  member_id CHAR(36) NOT NULL,
  coach_id CHAR(36) NOT NULL,
  PRIMARY KEY (member_id, coach_id),
  CONSTRAINT fk_member_user FOREIGN KEY (member_id) REFERENCES users(id) ON DELETE CASCADE,
  CONSTRAINT fk_coach_user FOREIGN KEY (coach_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS clubs (
  id CHAR(36) PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  coach_id CHAR(36) NOT NULL,
  sport VARCHAR(80) NOT NULL,
  active BOOLEAN NOT NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS training_plans (
  user_id CHAR(36) PRIMARY KEY,
  start_date DATE NOT NULL,
  ordinal_no INT NOT NULL
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS plan_days (
  id CHAR(36) PRIMARY KEY,
  user_id CHAR(36) NOT NULL,
  plan_date DATE NOT NULL,
  title VARCHAR(500) NOT NULL,
  status VARCHAR(80) NOT NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_plan_user_date (user_id, plan_date),
  CONSTRAINT fk_day_plan FOREIGN KEY (user_id) REFERENCES training_plans(user_id) ON DELETE CASCADE
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS exercises (
  day_id CHAR(36) NOT NULL,
  sequence_no INT NOT NULL,
  name VARCHAR(500) NOT NULL,
  muscle VARCHAR(100) NOT NULL,
  sets INT NOT NULL,
  reps INT NOT NULL,
  minutes INT NOT NULL,
  rest_seconds INT NOT NULL,
  PRIMARY KEY (day_id, sequence_no),
  CONSTRAINT fk_exercise_day FOREIGN KEY (day_id) REFERENCES plan_days(id) ON DELETE CASCADE
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS email_deliveries (
  id CHAR(36) PRIMARY KEY,
  user_id CHAR(36) NULL,
  recipient VARCHAR(254) NOT NULL,
  subject VARCHAR(500) NOT NULL,
  purpose VARCHAR(80) NOT NULL,
  status VARCHAR(40) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  sent_at DATETIME(6) NULL,
  message_id VARCHAR(254) NOT NULL,
  error_message VARCHAR(500) NOT NULL,
  INDEX ix_mail_created (created_at)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS identity_state (
  id TINYINT PRIMARY KEY,
  payload JSON NOT NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `diary_entries` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `measurements` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `reminders` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `notifications` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `messages` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `meal_assignments` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `meal_plans` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `meal_intakes` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `gyms` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `gym_memberships` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `gym_visits` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `workouts` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `weight_entries` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `food_entries` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `water_entries` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `matches` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `attendance` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `workout_templates` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `workout_drafts` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `program_changes` (
  record_key VARCHAR(160) PRIMARY KEY,
  user_id CHAR(36) NULL,
  record_date DATE NULL,
  amount DECIMAL(12,3) NULL,
  payload JSON NOT NULL,
  ordinal_no INT NOT NULL,
  INDEX ix_user_date (user_id,record_date)
) ENGINE=InnoDB;

INSERT IGNORE INTO app_state(id,revision,metadata) VALUES(1,0,JSON_OBJECT());
