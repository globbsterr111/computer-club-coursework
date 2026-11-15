-- База данных компьютерного клуба. SQL Server.
-- Выполнить один раз в новой пустой базе computer_club_db.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.roles
(
	role_id int PRIMARY KEY,
	role_name nvarchar(30) NOT NULL UNIQUE
);

CREATE TABLE dbo.computer_statuses
(
	computer_status_id int PRIMARY KEY,
	status_name nvarchar(30) NOT NULL UNIQUE
);

CREATE TABLE dbo.session_statuses
(
	session_status_id int PRIMARY KEY,
	status_name nvarchar(30) NOT NULL UNIQUE
);

CREATE TABLE dbo.payment_methods
(
	payment_method_id int PRIMARY KEY,
	method_name nvarchar(30) NOT NULL UNIQUE
);

CREATE TABLE dbo.clients
(
	client_id int IDENTITY(1, 1) PRIMARY KEY,
	last_name nvarchar(60) NOT NULL,
	first_name nvarchar(60) NOT NULL,
	middle_name nvarchar(60) NULL,
	phone nvarchar(20) NOT NULL UNIQUE,
	created_at datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.users
(
	user_id int IDENTITY(1, 1) PRIMARY KEY,
	login nvarchar(50) NOT NULL UNIQUE,
	password_hash nvarchar(500) NOT NULL,
	is_active bit NOT NULL DEFAULT 1,
	role_id int NOT NULL REFERENCES dbo.roles(role_id),
	client_id int NULL REFERENCES dbo.clients(client_id)
);

CREATE TABLE dbo.computers
(
	computer_id int IDENTITY(1, 1) PRIMARY KEY,
	computer_name nvarchar(30) NOT NULL UNIQUE,
	zone_name nvarchar(40) NOT NULL,
	computer_status_id int NOT NULL REFERENCES dbo.computer_statuses(computer_status_id)
);

CREATE TABLE dbo.tariffs
(
	tariff_id int IDENTITY(1, 1) PRIMARY KEY,
	tariff_name nvarchar(60) NOT NULL UNIQUE,
	price_per_hour decimal(10,2) NOT NULL CHECK(price_per_hour>0),
	is_active bit NOT NULL DEFAULT 1
);

CREATE TABLE dbo.sessions
(
	session_id int IDENTITY(1, 1) PRIMARY KEY,
	client_id int NOT NULL REFERENCES dbo.clients(client_id),
	computer_id int NOT NULL REFERENCES dbo.computers(computer_id),
	tariff_id int NOT NULL REFERENCES dbo.tariffs(tariff_id),
	start_time datetime2(0) NOT NULL,
	end_time datetime2(0) NULL,
	session_status_id int NOT NULL REFERENCES dbo.session_statuses(session_status_id),
	CONSTRAINT ck_session_state CHECK
	(
		(session_status_id = 1 AND end_time IS NULL)
		OR
		(session_status_id = 2 AND end_time IS NOT NULL
		 AND end_time >= start_time)
	)
);

CREATE TABLE dbo.payments
(
	payment_id int IDENTITY(1, 1) PRIMARY KEY,
	session_id int NOT NULL UNIQUE REFERENCES dbo.sessions(session_id),
	amount decimal(10,2) NOT NULL CHECK(amount>=0),
	payment_time datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
	payment_method_id int NOT NULL REFERENCES dbo.payment_methods(payment_method_id)
);

CREATE UNIQUE INDEX ux_users_client
	ON dbo.users(client_id)
	WHERE client_id IS NOT NULL;

CREATE UNIQUE INDEX ux_active_computer
	ON dbo.sessions(computer_id)
	WHERE end_time IS NULL;

CREATE UNIQUE INDEX ux_active_client
	ON dbo.sessions(client_id)
	WHERE end_time IS NULL;

INSERT INTO dbo.roles
VALUES
	(1,N'Администратор'),
	(2,N'Клиент');

INSERT INTO dbo.computer_statuses
VALUES
	(1,N'Исправен'),
	(2,N'Обслуживание');

INSERT INTO dbo.session_statuses
VALUES
	(1,N'Активен'),
	(2,N'Завершён');

INSERT INTO dbo.payment_methods
VALUES
	(1,N'Наличные'),
	(2,N'Банковская карта');

INSERT INTO dbo.clients (last_name,first_name,middle_name,phone)
VALUES
	(N'Иванов',N'Иван',N'Иванович',N'+79000000001'),
	(N'Петрова',N'Анна',NULL,N'+79000000002');

INSERT INTO dbo.computers (computer_name,zone_name,computer_status_id)
VALUES
	(N'PC-01',N'Основной',1),
	(N'PC-02',N'Основной',1),
	(N'PC-03',N'VIP',2);

INSERT INTO dbo.tariffs (tariff_name,price_per_hour)
VALUES
	(N'Стандарт',120),
	(N'VIP',180);

COMMIT;
GO

CREATE TRIGGER dbo.tr_tariff_history
ON dbo.tariffs
AFTER UPDATE
AS
BEGIN
	IF EXISTS
	(
		SELECT 1
		FROM inserted i
		JOIN deleted d ON i.tariff_id = d.tariff_id
		JOIN dbo.sessions s ON s.tariff_id = i.tariff_id
		WHERE i.price_per_hour <> d.price_per_hour
	)
		THROW 50001, N'Создайте новый тариф для новой цены', 1;
END;
GO
