import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { useMutation, useQuery } from '@apollo/client';
import { UPDATE_USER_PREFERENCES } from '../graphql/mutations';
import { GET_USER_PREFERENCES } from '../graphql/queries';
import { useAuth } from './AuthContext';

export type ThemeName = 'yasified' | 'boring' | 'retro90s' | 'dark' | 'ocean';

interface Theme {
  name: ThemeName;
  displayName: string;
  colors: {
    primaryColor: string;
    primaryHover: string;
    secondaryColor: string;
    dangerColor: string;
    successColor: string;
    background: string;
    cardBackground: string;
    borderColor: string;
    textPrimary: string;
    textSecondary: string;
    accent1: string;
    accent2: string;
    accent3: string;
  };
}

const themes: Record<ThemeName, Theme> = {
  yasified: {
    name: 'yasified',
    displayName: '💅 Yasified',
    colors: {
      primaryColor: '#ec4899',
      primaryHover: '#db2777',
      secondaryColor: '#8b5cf6',
      dangerColor: '#f43f5e',
      successColor: '#10b981',
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      cardBackground: 'rgba(255, 255, 255, 0.95)',
      borderColor: '#f0abfc',
      textPrimary: '#1e293b',
      textSecondary: '#64748b',
      accent1: '#fbbf24',
      accent2: '#34d399',
      accent3: '#60a5fa',
    },
  },
  boring: {
    name: 'boring',
    displayName: '📋 Professional',
    colors: {
      primaryColor: '#0ea5e9',
      primaryHover: '#0284c7',
      secondaryColor: '#10b981',
      dangerColor: '#ef4444',
      successColor: '#22c55e',
      background: '#f8fafc',
      cardBackground: '#ffffff',
      borderColor: '#e2e8f0',
      textPrimary: '#0f172a',
      textSecondary: '#64748b',
      accent1: '#06b6d4',
      accent2: '#14b8a6',
      accent3: '#10b981',
    },
  },
  retro90s: {
    name: 'retro90s',
    displayName: '🕹️ Retro 90s',
    colors: {
      primaryColor: '#ff00ff',
      primaryHover: '#cc00cc',
      secondaryColor: '#00ffff',
      dangerColor: '#ff0000',
      successColor: '#00ff00',
      background: 'repeating-linear-gradient(45deg, #ff1493 0px, #ff1493 20px, #00ced1 20px, #00ced1 40px, #ffd700 40px, #ffd700 60px, #7fff00 60px, #7fff00 80px)',
      cardBackground: '#ffffff',
      borderColor: '#000000',
      textPrimary: '#000000',
      textSecondary: '#666666',
      accent1: '#ffd700',
      accent2: '#00ff00',
      accent3: '#ff6347',
    },
  },
  dark: {
    name: 'dark',
    displayName: '🌙 Dark Mode',
    colors: {
      primaryColor: '#06b6d4',
      primaryHover: '#0891b2',
      secondaryColor: '#10b981',
      dangerColor: '#f87171',
      successColor: '#34d399',
      background: 'linear-gradient(135deg, #0f172a 0%, #020617 100%)',
      cardBackground: 'rgba(15, 23, 42, 0.95)',
      borderColor: '#1e293b',
      textPrimary: '#f1f5f9',
      textSecondary: '#94a3b8',
      accent1: '#06b6d4',
      accent2: '#34d399',
      accent3: '#10b981',
    },
  },
  ocean: {
    name: 'ocean',
    displayName: '🌊 Ocean Breeze',
    colors: {
      primaryColor: '#0ea5e9',
      primaryHover: '#0284c7',
      secondaryColor: '#06b6d4',
      dangerColor: '#f43f5e',
      successColor: '#14b8a6',
      background: 'linear-gradient(135deg, #e0f2fe 0%, #bae6fd 50%, #7dd3fc 100%)',
      cardBackground: 'rgba(255, 255, 255, 0.9)',
      borderColor: '#67e8f9',
      textPrimary: '#0c4a6e',
      textSecondary: '#0369a1',
      accent1: '#fbbf24',
      accent2: '#34d399',
      accent3: '#a78bfa',
    },
  },
};

interface ThemeContextType {
  theme: Theme;
  setTheme: (themeName: ThemeName) => void;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export const ThemeProvider = ({ children }: { children: ReactNode }) => {
  const { isAuthenticated } = useAuth();
  const [currentTheme, setCurrentTheme] = useState<ThemeName>(() => {
    const saved = localStorage.getItem('choreTrackerTheme') as ThemeName;
    return saved && saved in themes ? saved : 'yasified';
  });

  const theme = themes[currentTheme];

  // Fetch user preferences from backend
  const { data: prefsData } = useQuery(GET_USER_PREFERENCES, {
    skip: !isAuthenticated,
  });

  const [updatePreferences] = useMutation(UPDATE_USER_PREFERENCES);

  // Load theme from backend when user logs in
  useEffect(() => {
    if (prefsData?.userPreferences?.theme && prefsData.userPreferences.theme in themes) {
      setCurrentTheme(prefsData.userPreferences.theme as ThemeName);
    }
  }, [prefsData]);

  useEffect(() => {
    localStorage.setItem('choreTrackerTheme', currentTheme);

    // Apply CSS variables
    const root = document.documentElement;
    Object.entries(theme.colors).forEach(([key, value]) => {
      root.style.setProperty(`--${key.replace(/([A-Z])/g, '-$1').toLowerCase()}`, value);
    });
  }, [currentTheme, theme]);

  const setTheme = (themeName: ThemeName) => {
    setCurrentTheme(themeName);

    // Save to backend if authenticated
    if (isAuthenticated) {
      updatePreferences({
        variables: {
          theme: themeName,
        },
      }).catch((err) => {
        console.error('Failed to save theme preference:', err);
      });
    }
  };

  return (
    <ThemeContext.Provider value={{ theme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  );
};

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within ThemeProvider');
  }
  return context;
};

export { themes };
