# 🧹 Chore Tracker

A full-stack web application for tracking and managing household chores with smart frequency optimization based on necessity ratings.

## Features

### Core Functionality
- ✅ **Chore Management**: Create, edit, and delete chores across four categories
  - Daily: Recurring tasks with customizable intervals
  - Weekly: Tasks on specific days of the week
  - Monthly: Tasks on specific days of the month
  - Special: One-time tasks with deadlines

- ⭐ **Necessity Rating System**: Rate chores 1-5 after completion to track how necessary they actually were
  - 1 = Unnecessary
  - 2 = Could wait
  - 3 = Needed
  - 4 = Quite necessary
  - 5 = Critical

- 📊 **Analytics Dashboard**:
  - Completion trends over time
  - Category-based statistics
  - Average necessity ratings per chore
  - Top chores by importance
  - **Skip Analytics**: Track skipped chores and frequency optimization insights

- 🔔 **Smart Scheduling**: Automatically calculates next due dates based on recurrence patterns

- ⏭️ **Skip Tracking & Frequency Optimization**:
  - **Automatic Skip Detection**: When completing a chore late, the system automatically records all missed deadlines as skips
  - **Manual Skip Recording**: Explicitly skip chores with optional reasons
  - **Smart Frequency Recommendations**: Analyzes necessity ratings to suggest optimal chore frequency
    - Compares necessity ratings for on-time completions vs. completions after skips
    - If necessity is lower after skipping, suggests reducing frequency
    - If necessity is higher after skipping, current frequency is appropriate
  - **Skip Rate Analytics**: Track skip patterns to identify chores that may need schedule adjustments

## Tech Stack

### Backend
- **ASP.NET Core 9.0**: Web API framework
- **Hot Chocolate 15.1**: GraphQL server
- **Entity Framework Core**: ORM
- **PostgreSQL**: Database

### Frontend
- **React 18**: UI framework
- **TypeScript**: Type-safe JavaScript
- **Apollo Client**: GraphQL client
- **React Router**: Client-side routing
- **Recharts**: Data visualization
- **Vite**: Build tool

## Project Structure

```
Chores/
├── backend/                    # ASP.NET Core API
│   ├── Data/
│   │   └── ChoreTrackerDbContext.cs
│   ├── Models/
│   │   ├── Chore.cs
│   │   ├── ChoreCompletion.cs
│   │   ├── ChoreSkip.cs
│   │   ├── ChoreCategory.cs
│   │   └── RecurrencePattern.cs
│   ├── GraphQL/
│   │   ├── Queries/
│   │   │   └── Query.cs
│   │   ├── Mutations/
│   │   │   └── Mutation.cs
│   │   ├── Types/
│   │   │   ├── RecurrencePatternType.cs
│   │   │   └── ChoreSkipAnalytics.cs
│   │   └── Inputs/
│   │       ├── ChoreInput.cs
│   │       ├── CompleteChoreInput.cs
│   │       └── SkipChoreInput.cs
│   └── Program.cs
│
└── frontend/                   # React app
    ├── src/
    │   ├── components/
    │   │   ├── ChoreList.tsx
    │   │   ├── ChoreItem.tsx
    │   │   ├── ChoreForm.tsx
    │   │   ├── CompleteChoreModal.tsx
    │   │   └── Dashboard.tsx
    │   ├── graphql/
    │   │   ├── queries.ts
    │   │   └── mutations.ts
    │   ├── types.ts
    │   ├── apolloClient.ts
    │   ├── App.tsx
    │   └── App.css
    └── package.json
```

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Node.js 18+ and npm
- PostgreSQL 12+

### Backend Setup

1. **Install PostgreSQL and create a database:**
   ```bash
   createdb choretracker
   ```

2. **Navigate to backend directory:**
   ```bash
   cd backend
   ```

3. **Update connection string** in `appsettings.json` if needed:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=choretracker;Username=postgres;Password=postgres"
     }
   }
   ```

4. **Run database migrations:**
   ```bash
   dotnet ef database update
   ```

5. **Start the backend:**
   ```bash
   dotnet run
   ```

   The API will be available at `https://localhost:5000/graphql`

### Frontend Setup

1. **Navigate to frontend directory:**
   ```bash
   cd frontend
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Create a `.env` file** (optional, defaults to localhost:5000):
   ```env
   VITE_GRAPHQL_URI=http://localhost:5000/graphql
   ```

4. **Start the development server:**
   ```bash
   npm run dev
   ```

   The app will be available at `http://localhost:5173`

## GraphQL API

### Queries

```graphql
# Get all chores
query {
  chores {
    id
    name
    category
    completions {
      necessityRating
    }
  }
}

# Get upcoming chores
query {
  upcomingChores(daysAhead: 7) {
    id
    name
    deadline
  }
}

# Get completion history
query {
  completionHistory {
    id
    completionDate
    necessityRating
    chore {
      name
    }
  }
}

# Get skip analytics
query {
  choreSkipAnalytics {
    choreId
    choreName
    totalSkips
    totalCompletions
    skipRate
    averageNecessityAfterSkip
    averageNecessityOnTime
    necessityDifference
    frequencyRecommendation
  }
}
```

### Mutations

```graphql
# Create a chore
mutation {
  createChore(input: {
    name: "Sweep floors"
    category: DAILY
    recurrencePattern: {
      dayInterval: 2
    }
  }) {
    id
    name
  }
}

# Complete a chore with rating
mutation {
  completeChore(input: {
    choreId: 1
    necessityRating: 4
    notes: "Floors were quite dirty"
  }) {
    id
    completionDate
  }
}

# Skip a chore
mutation {
  skipChore(input: {
    choreId: 1
    skippedDeadline: "2025-10-01"
    reason: "Too busy this week"
  }) {
    id
    skippedDeadline
    reason
  }
}
```

## Database Schema

### Chores Table
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| Name | string | Chore name |
| Description | string | Optional description |
| Category | enum | Daily/Weekly/Monthly/Special |
| RecurrencePattern | jsonb | Recurrence details |
| Deadline | datetime | For special chores |
| CreatedDate | datetime | Creation timestamp |
| IsActive | boolean | Soft delete flag |

### ChoreCompletions Table
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| ChoreId | int | Foreign key to Chores |
| CompletionDate | datetime | When completed |
| NecessityRating | int | 1-5 rating |
| Notes | string | Optional notes |

### ChoreSkips Table
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| ChoreId | int | Foreign key to Chores |
| UserId | int | Foreign key to Users |
| SkippedDeadline | datetime | The deadline that was missed |
| RecordedDate | datetime | When skip was recorded |
| Reason | string | Optional reason (manual/automatic) |

## Usage Examples

### Adding a Daily Chore
1. Click "Add Chore"
2. Enter name: "Water plants"
3. Select category: "Daily"
4. Set interval: Every 2 days
5. Click "Create Chore"

### Completing a Chore
1. Find chore in list
2. Click "Complete" button
3. Rate necessity (1-5)
4. Add optional notes
5. Click "Complete Chore"

### Viewing Analytics
1. Navigate to "Dashboard"
2. View completion trends
3. Check category breakdowns
4. See top chores by necessity
5. Review skip analytics for frequency insights

### Understanding Skip Analytics
The skip analytics feature helps optimize chore frequency by:
1. **Automatic Detection**: When you complete a chore late, missed deadlines are automatically recorded as skips
2. **Necessity Comparison**: The system compares necessity ratings for:
   - Completions after skips
   - On-time completions
3. **Recommendations**:
   - **Lower necessity after skips** → Consider reducing frequency
   - **Higher necessity after skips** → Current frequency is appropriate
   - **High skip rate** → May need schedule adjustment

## Future Enhancements

- 📱 Mobile app (React Native)
- 🔔 Push notifications for overdue chores
- 👥 Multi-user support with household sharing
- 📤 Data export (CSV/PDF)
- 🌐 i18n for multiple languages

## Development

### Build for Production

**Backend:**
```bash
cd backend
dotnet publish -c Release
```

**Frontend:**
```bash
cd frontend
npm run build
```

### Testing GraphQL API

Open `https://localhost:5000/graphql` in your browser to access the GraphQL Playground (Banana Cake Pop) for testing queries and mutations.

## License

MIT License - feel free to use this project for your own chore tracking needs!

## Contributing

Contributions welcome! Please feel free to submit a Pull Request.
