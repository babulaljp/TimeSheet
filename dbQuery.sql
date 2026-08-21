create database TimeSheet;

use TimeSheet;

create table users (
	userid INT PRIMARY KEY identity(1,1),
	username VARCHAR(50) NOT NULL UNIQUE,
	UserPassword VARCHAR(255) NOT NULL,
	FirstName VARCHAR(50) NOT NULL,
	LastName VARCHAR(50) NOT NULL,
	Email VARCHAR(100) NOT NULL UNIQUE,
	created_at datetime DEFAULT getdate()
);

insert into users (username, UserPassword, FirstName, LastName, Email) values
('admin', 'hashed_admin_1', 'Admin', 'Admin', 'admin@example.com');

select * from users; 
---------------------

create table Projects (ProjectId INT PRIMARY KEY identity(1,1), ProjectName VARCHAR(255) NOT NULL) 

insert into Projects (ProjectName) values ('United Educators')
 
SELECT ProjectId, ProjectName FROM Projects ORDER BY ProjectName

---------------------

create table Tasks (TaskId  INT PRIMARY KEY identity(1,1), TaskName VARCHAR(255) NOT NULL)

insert into Tasks (TaskName) values ('Developement')
insert into Tasks (TaskName) values ('BA')
insert into Tasks (TaskName) values ('Testing')

SELECT TaskId, TaskName FROM Tasks ORDER BY TaskName
-------------------------
create table UserTime
(UTid INT PRIMARY KEY identity(1,1),
[UserId] int,
[TaskId] int ,
[ProjectId] int ,
[Details] varchar(max),
[tHours] float,
[tDate] datetime,
[updateAt] datetime DEFAULT getdate()

)

select * from [dbo].[UserTime]  
-------------.
create table Clients (ClientId INT PRIMARY KEY identity(1,1), ClientName VARCHAR(255) NOT NULL)
insert into Clients (ClientName) values ('United Educators');
-------------

select ut.tDate, ut.tHours, ut.Details, p.ProjectName, t.TaskName
from UserTime ut
join Tasks t on ut.TaskId = t.TaskId
join Projects p on ut.ProjectId = p.ProjectId
join Users u on ut.UserId = u.UserId
where u.UserName = 'babulaljp' and CONVERT(date, ut.tDate) = convert(date,getdate())
order by ut.updateAt asc;