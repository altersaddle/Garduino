USE [garden]
GO

/****** Object:  Table [dbo].[SENSOR_DESC]    Script Date: 7/20/2013 10:15:56 AM ******/
DROP TABLE [dbo].[SENSOR_DESC]
GO
/****** Object:  Table [dbo].[SENSORDATA]    Script Date: 7/20/2013 10:15:28 AM ******/
DROP TABLE [dbo].[SENSORDATA]
GO

/****** Object:  Table [dbo].[SENSOR_DESC]    Script Date: 7/20/2013 10:15:56 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SENSOR_DESC](
	[ID] [numeric](18, 0) IDENTITY(1,1) NOT NULL,
	[SENSOR] [nchar](16) NOT NULL,
	[DESCRIPTION] [nchar](255) NULL
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[SENSORDATA]    Script Date: 7/20/2013 10:15:28 AM ******/
CREATE TABLE [dbo].[SENSORDATA](
	[ID] [numeric](18, 0) IDENTITY(1,1) NOT NULL,
	[DEVICE] [nchar](16) NOT NULL,
	[SENSOR] [nchar](16) NOT NULL,
	[NUMVAL] [numeric](18, 4) NULL,
	[STRVAL] [nchar](255) NULL,
	[OBSERVED] [datetime] NULL,
	[COLLECTED] [datetime] NULL
) ON [PRIMARY]

GO

CREATE NONCLUSTERED INDEX IDX_SENSOR_NUM_COLL
ON [dbo].[SENSORDATA] ([DEVICE],[SENSOR])
INCLUDE ([NUMVAL],[COLLECTED])
GO
