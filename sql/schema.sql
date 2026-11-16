-- TalentPulse star schema (SQL Server / Azure SQL).
-- Optional: Power BI can read the CSV files directly; this is for a warehouse-style setup.
-- Load the data afterwards with load-sqlserver.sql.

CREATE SCHEMA hr;
GO

-- ---------------------------------------------------------------- dimensions

CREATE TABLE hr.dim_date (
    [Date]        date        NOT NULL PRIMARY KEY,
    [Year]        smallint    NOT NULL,
    [Quarter]     char(2)     NOT NULL,
    MonthNumber   tinyint     NOT NULL,
    MonthName     char(3)     NOT NULL,
    YearMonth     char(7)     NOT NULL,
    MonthStart    date        NOT NULL,
    IsMonthEnd    bit         NOT NULL
);

CREATE TABLE hr.dim_location (
    LocationKey   int           NOT NULL PRIMARY KEY,
    City          nvarchar(64)  NOT NULL,
    Country       nvarchar(64)  NOT NULL,
    Region        nvarchar(32)  NOT NULL
);

CREATE TABLE hr.dim_org_unit (
    OrgUnitKey    int           NOT NULL PRIMARY KEY,
    [Function]    nvarchar(64)  NOT NULL,
    Department    nvarchar(64)  NOT NULL
);

CREATE TABLE hr.dim_job (
    JobKey        int           NOT NULL PRIMARY KEY,
    JobTitle      nvarchar(128) NOT NULL,
    JobFamily     nvarchar(64)  NOT NULL,
    [Level]       nvarchar(16)  NOT NULL,
    LevelOrder    tinyint       NOT NULL,
    Grade         tinyint       NOT NULL,
    OrgUnitKey    int           NOT NULL REFERENCES hr.dim_org_unit (OrgUnitKey)
);

CREATE TABLE hr.dim_source (
    SourceKey      int           NOT NULL PRIMARY KEY,
    Source         nvarchar(64)  NOT NULL,
    Channel        nvarchar(16)  NOT NULL,
    CostPerHireEur decimal(12,2) NOT NULL
);

CREATE TABLE hr.dim_employee (
    EmployeeKey       int           NOT NULL PRIMARY KEY,
    EmployeeCode      varchar(8)    NOT NULL UNIQUE,
    Gender            nvarchar(16)  NOT NULL,
    BirthYear         smallint      NOT NULL,
    HireDate          date          NOT NULL,
    TerminationDate   date          NULL,
    TerminationType   nvarchar(16)  NULL,
    TerminationReason nvarchar(64)  NULL,
    HireSourceKey     int           NOT NULL REFERENCES hr.dim_source (SourceKey),
    WorkModel         nvarchar(16)  NOT NULL,
    EmploymentType    nvarchar(16)  NOT NULL,
    FTE               decimal(3,2)  NOT NULL,
    OrgUnitKey        int           NOT NULL REFERENCES hr.dim_org_unit (OrgUnitKey),
    LocationKey       int           NOT NULL REFERENCES hr.dim_location (LocationKey),
    CurrentJobKey     int           NOT NULL REFERENCES hr.dim_job (JobKey),
    IsActive          bit           NOT NULL
);

CREATE TABLE hr.security_user_country (
    UserEmail     nvarchar(128) NOT NULL,
    Country       nvarchar(64)  NOT NULL,
    PRIMARY KEY (UserEmail, Country)
);

-- ---------------------------------------------------------------- facts

CREATE TABLE hr.fact_headcount_monthly (
    MonthEnd      date         NOT NULL REFERENCES hr.dim_date ([Date]),
    EmployeeKey   int          NOT NULL REFERENCES hr.dim_employee (EmployeeKey),
    JobKey        int          NOT NULL REFERENCES hr.dim_job (JobKey),
    OrgUnitKey    int          NOT NULL REFERENCES hr.dim_org_unit (OrgUnitKey),
    LocationKey   int          NOT NULL REFERENCES hr.dim_location (LocationKey),
    FTE           decimal(3,2) NOT NULL,
    TenureMonths  smallint     NOT NULL,
    Age           tinyint      NOT NULL,
    PRIMARY KEY (MonthEnd, EmployeeKey)
);

CREATE TABLE hr.fact_employee_event (
    [Date]        date          NOT NULL REFERENCES hr.dim_date ([Date]),
    EmployeeKey   int           NOT NULL REFERENCES hr.dim_employee (EmployeeKey),
    EventType     nvarchar(16)  NOT NULL,
    JobKey        int           NOT NULL REFERENCES hr.dim_job (JobKey),
    OrgUnitKey    int           NOT NULL REFERENCES hr.dim_org_unit (OrgUnitKey),
    LocationKey   int           NOT NULL REFERENCES hr.dim_location (LocationKey),
    FromJobKey    int           NULL,
    ToJobKey      int           NULL,
    Detail        nvarchar(128) NULL
);

CREATE TABLE hr.fact_requisition (
    RequisitionKey      int           NOT NULL PRIMARY KEY,
    RequisitionCode     varchar(12)   NOT NULL UNIQUE,
    JobKey              int           NOT NULL REFERENCES hr.dim_job (JobKey),
    OrgUnitKey          int           NOT NULL REFERENCES hr.dim_org_unit (OrgUnitKey),
    LocationKey         int           NOT NULL REFERENCES hr.dim_location (LocationKey),
    Reason              nvarchar(16)  NOT NULL,
    Status              nvarchar(16)  NOT NULL,
    OpenedDate          date          NOT NULL,
    FilledDate          date          NULL,
    StartDate           date          NULL,
    CancelledDate       date          NULL,
    HireSourceKey       int           NULL REFERENCES hr.dim_source (SourceKey),
    HireCostEur         decimal(12,2) NULL,
    HiredEmployeeKey    int           NULL,
    HiredApplicationKey int           NULL,
    DaysToFill          int           NULL,
    DaysOpen            int           NULL
);

CREATE TABLE hr.fact_application (
    ApplicationKey      int           NOT NULL PRIMARY KEY,
    RequisitionKey      int           NOT NULL REFERENCES hr.fact_requisition (RequisitionKey),
    JobKey              int           NOT NULL REFERENCES hr.dim_job (JobKey),
    OrgUnitKey          int           NOT NULL REFERENCES hr.dim_org_unit (OrgUnitKey),
    LocationKey         int           NOT NULL REFERENCES hr.dim_location (LocationKey),
    CandidateCode       varchar(8)    NOT NULL,
    SourceKey           int           NOT NULL REFERENCES hr.dim_source (SourceKey),
    AppliedDate         date          NOT NULL,
    ScreenDate          date          NULL,
    InterviewDate       date          NULL,
    OfferDate           date          NULL,
    OfferAcceptedDate   date          NULL,
    StartDate           date          NULL,
    Outcome             nvarchar(16)  NOT NULL,
    FurthestStage       nvarchar(16)  NOT NULL,
    FurthestStageOrder  tinyint       NOT NULL,
    ReachedScreen       bit           NOT NULL,
    ReachedInterview    bit           NOT NULL,
    ReachedOffer        bit           NOT NULL,
    IsHire              bit           NOT NULL,
    DaysToHire          int           NULL
);

CREATE INDEX IX_headcount_org ON hr.fact_headcount_monthly (OrgUnitKey, LocationKey, MonthEnd);
CREATE INDEX IX_event_type_date ON hr.fact_employee_event (EventType, [Date]);
CREATE INDEX IX_application_req ON hr.fact_application (RequisitionKey);
