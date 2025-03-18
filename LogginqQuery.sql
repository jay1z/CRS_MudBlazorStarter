SELECT TOP (100) [Id]
      ,[Message]
      ,[MessageTemplate]
      ,[Level]
      ,[TimeStamp]
      ,[Exception]
      ,[Properties]
  FROM [alx_dashboard].[dbo].[LogEvents]
  ORDER BY [Id] DESC