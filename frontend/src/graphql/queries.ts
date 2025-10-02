import { gql } from '@apollo/client';

export const GET_CHORES = gql`
  query GetChores {
    chores {
      id
      name
      description
      location
      category
      recurrencePattern {
        dayInterval
        daysOfWeek
        dayOfMonth
        relativePattern
        endDate
      }
      deadline
      createdDate
      isActive
      isCompletedForPeriod
      completions {
        id
        completionDate
        necessityRating
        notes
      }
    }
  }
`;

export const GET_CHORE = gql`
  query GetChore($id: Int!) {
    chore(id: $id) {
      id
      name
      description
      location
      category
      recurrencePattern {
        dayInterval
        daysOfWeek
        dayOfMonth
        relativePattern
        endDate
      }
      deadline
      createdDate
      isActive
      isCompletedForPeriod
      completions {
        id
        completionDate
        necessityRating
        notes
      }
    }
  }
`;

export const GET_UPCOMING_CHORES = gql`
  query GetUpcomingChores($daysAhead: Int!) {
    upcomingChores(daysAhead: $daysAhead) {
      id
      name
      description
      category
      deadline
      completions {
        id
        completionDate
        necessityRating
      }
    }
  }
`;

export const GET_COMPLETION_HISTORY = gql`
  query GetCompletionHistory {
    completionHistory {
      id
      choreId
      completionDate
      necessityRating
      notes
      chore {
        id
        name
        category
        location
      }
    }
  }
`;

export const GET_USER_PREFERENCES = gql`
  query GetUserPreferences {
    userPreferences {
      id
      dayStartHour
      theme
      createdDate
      modifiedDate
    }
  }
`;

export const GET_CHORE_SKIP_ANALYTICS = gql`
  query GetChoreSkipAnalytics {
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
`;
