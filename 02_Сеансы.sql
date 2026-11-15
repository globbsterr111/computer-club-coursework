CREATE OR ALTER VIEW dbo.v_session_costs AS
SELECT s.*, CAST(ROUND(CEILING(DATEDIFF_BIG(SECOND,s.start_time,s.end_time)/60.0)*t.price_per_hour/60.0,2) AS decimal(10,2)) AS total_cost,
CAST(CASE WHEN p.payment_id IS NULL THEN 0 ELSE 1 END AS bit) AS is_paid
FROM dbo.sessions s JOIN dbo.tariffs t ON s.tariff_id=t.tariff_id LEFT JOIN dbo.payments p ON p.session_id=s.session_id;
GO
CREATE OR ALTER PROCEDURE dbo.start_session @client_id int, @computer_id int, @tariff_id int AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRAN;
 IF NOT EXISTS(SELECT 1 FROM dbo.computers WITH(UPDLOCK,HOLDLOCK) WHERE computer_id=@computer_id AND computer_status_id=1) THROW 50002,N'Компьютер недоступен',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.tariffs WITH(UPDLOCK,HOLDLOCK) WHERE tariff_id=@tariff_id AND is_active=1) THROW 50003,N'Тариф недоступен',1;
 INSERT dbo.sessions(client_id,computer_id,tariff_id,start_time,session_status_id) VALUES(@client_id,@computer_id,@tariff_id,SYSUTCDATETIME(),1);
 SELECT CAST(SCOPE_IDENTITY() AS int) AS session_id;
 COMMIT;
 END TRY
 BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
 END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.finish_session @session_id int AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 UPDATE dbo.sessions SET end_time=SYSUTCDATETIME(),session_status_id=2 WHERE session_id=@session_id AND end_time IS NULL;
 IF @@ROWCOUNT=0 THROW 50004,N'Активный сеанс не найден',1;
 SELECT * FROM dbo.v_session_costs WHERE session_id=@session_id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.pay_session @session_id int,@payment_method_id int AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRAN;
 DECLARE @cost decimal(10,2);
 SELECT @cost=total_cost FROM dbo.v_session_costs WITH(UPDLOCK,HOLDLOCK) WHERE session_id=@session_id AND end_time IS NOT NULL;
 IF @cost IS NULL THROW 50005,N'Сначала завершите сеанс',1;
 INSERT dbo.payments(session_id,amount,payment_method_id) VALUES(@session_id,@cost,@payment_method_id);
 COMMIT;
 END TRY
 BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
 END CATCH
END;
GO

