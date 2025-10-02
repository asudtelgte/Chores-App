import { useTheme, themes } from '../contexts/ThemeContext';
import type { ThemeName } from '../contexts/ThemeContext';

const ThemeSelector = () => {
  const { theme, setTheme } = useTheme();

  return (
    <div className="theme-selector">
      <select
        value={theme.name}
        onChange={(e) => setTheme(e.target.value as ThemeName)}
        className="theme-select"
      >
        {Object.values(themes).map((t) => (
          <option key={t.name} value={t.name}>
            {t.displayName}
          </option>
        ))}
      </select>
    </div>
  );
};

export default ThemeSelector;
