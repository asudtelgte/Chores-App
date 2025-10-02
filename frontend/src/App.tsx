import { ApolloProvider } from '@apollo/client';
import { BrowserRouter, Routes, Route, Link, Navigate, useNavigate } from 'react-router-dom';
import { useEffect } from 'react';
import { client } from './apolloClient';
import ChoreList from './components/ChoreList';
import ChoreForm from './components/ChoreForm';
import Dashboard from './components/Dashboard';
import Preferences from './components/Preferences';
import Auth from './components/Auth';
import { useTheme, ThemeProvider } from './contexts/ThemeContext';
import { useAuth, AuthProvider } from './contexts/AuthContext';
import { ToastProvider, useToast } from './contexts/ToastContext';
import './App.css';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <div>Loading...</div>;
  }

  return isAuthenticated ? <>{children}</> : <Navigate to="/auth" />;
}

function AppContent() {
  const { theme } = useTheme();
  const { isAuthenticated, logout, user } = useAuth();
  const { showToast } = useToast();
  const navigate = useNavigate();

  // Listen for auth errors and show toast
  useEffect(() => {
    const handleAuthError = (event: Event) => {
      const customEvent = event as CustomEvent<{ message: string }>;
      showToast(customEvent.detail.message, 'error');
    };

    window.addEventListener('auth-error', handleAuthError);

    return () => {
      window.removeEventListener('auth-error', handleAuthError);
    };
  }, [showToast]);

  return (
    <div className="app" data-theme={theme.name}>
      {isAuthenticated && (
        <nav className="navbar">
          <h1>🧹 Chore Tracker</h1>
          <div className="nav-links">
            <Link to="/">Chores</Link>
            <Link to="/dashboard">Dashboard</Link>
            <span
              className="user-info"
              title={user?.email}
              onClick={() => navigate('/preferences')}
              style={{ cursor: 'pointer' }}
            >
              👤
            </span>
            <button onClick={logout} className="btn-logout">
              Logout
            </button>
          </div>
        </nav>
      )}

      <main className="main-content">
        <Routes>
          <Route path="/auth" element={<Auth />} />
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <ChoreList />
              </ProtectedRoute>
            }
          />
          <Route
            path="/chores/new"
            element={
              <ProtectedRoute>
                <ChoreForm />
              </ProtectedRoute>
            }
          />
          <Route
            path="/chores/:id/edit"
            element={
              <ProtectedRoute>
                <ChoreForm />
              </ProtectedRoute>
            }
          />
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            }
          />
          <Route
            path="/preferences"
            element={
              <ProtectedRoute>
                <Preferences />
              </ProtectedRoute>
            }
          />
        </Routes>
      </main>
    </div>
  );
}

function App() {
  return (
    <ApolloProvider client={client}>
      <BrowserRouter>
        <ToastProvider>
          <AuthProvider>
            <ThemeProvider>
              <AppContent />
            </ThemeProvider>
          </AuthProvider>
        </ToastProvider>
      </BrowserRouter>
    </ApolloProvider>
  );
}

export default App;
