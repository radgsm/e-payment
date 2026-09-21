-- Database for the EPaymentMiddleware
-- Run this script in SQL Server to create the database and the PaymentOrders table.

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'EPaymentMiddlewareDb')
BEGIN
    CREATE DATABASE EPaymentMiddlewareDb;
END
GO

USE EPaymentMiddlewareDb;
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'PaymentOrders')
BEGIN
    CREATE TABLE [dbo].[PaymentOrders] (
        [Id]                    INT IDENTITY (1, 1) NOT NULL,
        [SessionId]             NVARCHAR(100) NOT NULL,
        [OrderNumber]           NVARCHAR(50)  NOT NULL,
        [OrderId]               NVARCHAR(100) NULL,
        [Amount]                INT NOT NULL DEFAULT 0,
        [Lang]                  NVARCHAR(10)  NULL,
        [PaymentMethod]         INT NOT NULL DEFAULT 1,
        [ClientId]              NVARCHAR(50)  NULL,
        [Status]                INT NOT NULL DEFAULT 0,
        [approvalCode]          NVARCHAR(50)  NULL,
        [respCode]              NVARCHAR(10)  NULL,
        [ErrorCode]             NVARCHAR(10)  NULL,
        [OrderStatus]           INT NOT NULL DEFAULT 0,
        [actionCodeDescription] NVARCHAR(300) NULL,
        [JsonParams]            NVARCHAR(500) NULL,
        [CreatedAt]             DATETIME NOT NULL DEFAULT GETDATE(),
        [ConfirmedAt]           DATETIME NULL,
        CONSTRAINT [PK_PaymentOrders] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE UNIQUE INDEX IX_PaymentOrders_OrderNumber ON [dbo].[PaymentOrders]([OrderNumber]);
    CREATE INDEX IX_PaymentOrders_SessionId ON [dbo].[PaymentOrders]([SessionId]);
    CREATE INDEX IX_PaymentOrders_OrderId ON [dbo].[PaymentOrders]([OrderId]);
END
GO
