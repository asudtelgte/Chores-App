import { useState } from 'react';
import { useMutation } from '@apollo/client';
import { useNavigate } from 'react-router-dom';
import { LOGIN, REGISTER } from '../graphql/mutations';
import { useAuth } from '../contexts/AuthContext';

const Auth = () => {
  const [isLogin, setIsLogin] = useState(true);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');

  const navigate = useNavigate();
  const { login: authLogin } = useAuth();

  const [loginMutation, { loading: loginLoading }] = useMutation(LOGIN);
  const [registerMutation, { loading: registerLoading }] = useMutation(REGISTER);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    try {
      if (isLogin) {
        const { data } = await loginMutation({
          variables: {
            input: {
              email,
              password,
            },
          },
        });

        if (data?.login) {
          authLogin(data.login.token, data.login.user);
          navigate('/');
        }
      } else {
        if (password.length < 8) {
          setError('Password must be at least 8 characters long');
          return;
        }

        const { data } = await registerMutation({
          variables: {
            input: {
              email,
              password,
            },
          },
        });

        if (data?.register) {
          authLogin(data.register.token, data.register.user);
          navigate('/');
        }
      }
    } catch (err: any) {
      setError(err.message || 'An error occurred');
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-card">
        <h1>{isLogin ? '🔐 Login' : '✨ Create Account'}</h1>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              type="email"
              id="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoFocus
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <div className="password-input-wrapper">
              <input
                type={showPassword ? 'text' : 'password'}
                id="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                minLength={8}
              />
              <button
                type="button"
                className="password-toggle"
                onClick={() => setShowPassword(!showPassword)}
                aria-label={showPassword ? 'Hide password' : 'Show password'}
              >
                {showPassword ? '👁️' : '👁️‍🗨️'}
              </button>
            </div>
            {!isLogin && (
              <small className="form-hint">
                Must be at least 8 characters with uppercase, lowercase, number, and special character
              </small>
            )}
          </div>

          {error && <div className="error-message">{error}</div>}

          <button
            type="submit"
            className="btn-primary"
            disabled={loginLoading || registerLoading}
          >
            {loginLoading || registerLoading
              ? 'Loading...'
              : isLogin
              ? 'Login'
              : 'Create Account'}
          </button>
        </form>

        <div className="auth-toggle">
          {isLogin ? "Don't have an account? " : 'Already have an account? '}
          <button
            type="button"
            className="link-button"
            onClick={() => {
              setIsLogin(!isLogin);
              setError('');
            }}
          >
            {isLogin ? 'Sign up' : 'Login'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default Auth;
