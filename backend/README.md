# Chore Tracker API

Backend API for the Chore Tracker application built with ASP.NET Core, Hot Chocolate GraphQL, and PostgreSQL.

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL 12+

## Setup

1. Install PostgreSQL and create a database:
```bash
createdb choretracker
```

2. Update connection string in `appsettings.json` if needed

3. Run migrations:
```bash
dotnet ef database update
```

4. Run the application:
```bash
dotnet run
```

The GraphQL endpoint will be available at: `https://localhost:5001/graphql`

## GraphQL Schema

### Queries

- `chores` - Get all chores (supports filtering and sorting)
- `chore(id: Int!)` - Get a single chore by ID
- `upcomingChores(daysAhead: Int!)` - Get chores due in the next X days
- `completionHistory` - Get completion history (supports filtering)

### Mutations

- `createChore(input: ChoreInput!)` - Create a new chore
- `updateChore(id: Int!, input: ChoreInput!)` - Update an existing chore
- `deleteChore(id: Int!)` - Soft delete a chore
- `completeChore(input: CompleteChoreInput!)` - Mark a chore as complete with rating
- `updateCompletionRating(completionId: Int!, rating: Int!, notes: String)` - Update a completion rating

## Database Schema

### Chores Table
- Id (PK)
- Name
- Description
- Category (Daily/Weekly/Monthly/Special)
- RecurrencePattern (JSON)
- Deadline
- CreatedDate
- IsActive

### ChoreCompletions Table
- Id (PK)
- ChoreId (FK)
- CompletionDate
- NecessityRating (1-5)
- Notes
