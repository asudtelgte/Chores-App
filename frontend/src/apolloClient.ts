import { ApolloClient, InMemoryCache, HttpLink, from } from '@apollo/client';
import { setContext } from '@apollo/client/link/context';
import { onError } from '@apollo/client/link/error';

const httpLink = new HttpLink({
  uri: import.meta.env.VITE_GRAPHQL_URI || 'http://localhost:5286/graphql',
});

const authLink = setContext((_, { headers }) => {
  const token = localStorage.getItem('authToken');
  return {
    headers: {
      ...headers,
      authorization: token ? `Bearer ${token}` : '',
    },
  };
});

// Error handling link - detects auth errors and triggers logout
const errorLink = onError(({ graphQLErrors, networkError }) => {
  if (graphQLErrors) {
    for (const err of graphQLErrors) {
      // Check for authentication errors - look in both message and extensions
      const errorMessage = err.extensions?.message as string | undefined;

      if (
        err.message.includes('User is not authenticated') ||
        err.message.includes('Unauthorized') ||
        errorMessage?.includes('User is not authenticated') ||
        errorMessage?.includes('Unauthorized') ||
        err.extensions?.code === 'AUTH_NOT_AUTHENTICATED'
      ) {
        // Clear auth data
        localStorage.removeItem('authToken');
        localStorage.removeItem('authUser');

        // Dispatch a custom event that the AuthContext can listen to
        window.dispatchEvent(new CustomEvent('auth-error', {
          detail: { message: 'Your session has expired. Please log in again.' }
        }));

        // Reload to redirect to login
        window.location.href = '/auth';
        break;
      }
    }
  }

  if (networkError && 'statusCode' in networkError) {
    // Handle 401/403 network errors
    if (networkError.statusCode === 401 || networkError.statusCode === 403) {
      localStorage.removeItem('authToken');
      localStorage.removeItem('authUser');

      window.dispatchEvent(new CustomEvent('auth-error', {
        detail: { message: 'Your session has expired. Please log in again.' }
      }));

      window.location.href = '/auth';
    }
  }
});

export const client = new ApolloClient({
  link: from([errorLink, authLink, httpLink]),
  cache: new InMemoryCache(),
  defaultOptions: {
    watchQuery: {
      fetchPolicy: 'network-only',
    },
  },
});
