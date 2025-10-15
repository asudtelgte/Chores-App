import { gql } from '@apollo/client';

export const REGISTER = gql`
  mutation Register($input: RegisterInput!) {
    register(input: $input) {
      token
      user {
        id
        email
      }
    }
  }
`;

export const LOGIN = gql`
  mutation Login($input: LoginInput!) {
    login(input: $input) {
      token
      user {
        id
        email
      }
    }
  }
`;

export const CREATE_CHORE = gql`
  mutation CreateChore($input: ChoreInput!) {
    createChore(input: $input) {
      id
      name
      description
      location
      category
      recurrencePattern {
        dayInterval
        weekInterval
        daysOfWeek
        monthInterval
        dayOfMonth
        relativePattern
        endDate
      }
      deadline
      createdDate
      isActive
    }
  }
`;

export const UPDATE_CHORE = gql`
  mutation UpdateChore($id: Int!, $input: ChoreInput!) {
    updateChore(id: $id, input: $input) {
      id
      name
      description
      location
      category
      recurrencePattern {
        dayInterval
        weekInterval
        daysOfWeek
        monthInterval
        dayOfMonth
        relativePattern
        endDate
      }
      deadline
      isActive
    }
  }
`;

export const DELETE_CHORE = gql`
  mutation DeleteChore($id: Int!) {
    deleteChore(id: $id)
  }
`;

export const COMPLETE_CHORE = gql`
  mutation CompleteChore($input: CompleteChoreInput!) {
    completeChore(input: $input) {
      id
      choreId
      completionDate
      necessityRating
      notes
      chore {
        id
        name
      }
    }
  }
`;

export const UPDATE_COMPLETION_RATING = gql`
  mutation UpdateCompletionRating($completionId: Int!, $rating: Int!, $notes: String) {
    updateCompletionRating(completionId: $completionId, rating: $rating, notes: $notes) {
      id
      necessityRating
      notes
    }
  }
`;

export const UPDATE_USER_PREFERENCES = gql`
  mutation UpdateUserPreferences($dayStartHour: Int, $theme: String) {
    updateUserPreferences(dayStartHour: $dayStartHour, theme: $theme) {
      id
      dayStartHour
      theme
      createdDate
      modifiedDate
    }
  }
`;

export const SKIP_CHORE = gql`
  mutation SkipChore($input: SkipChoreInput!) {
    skipChore(input: $input) {
      id
      choreId
      skippedDeadline
      reason
      recordedDate
    }
  }
`;
