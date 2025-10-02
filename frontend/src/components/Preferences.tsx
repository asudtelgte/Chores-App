import { useState, useEffect } from 'react';
import { useQuery, useMutation } from '@apollo/client';
import { GET_USER_PREFERENCES } from '../graphql/queries';
import { UPDATE_USER_PREFERENCES } from '../graphql/mutations';
import ThemeSelector from './ThemeSelector';

interface UserPreferences {
  id: number;
  dayStartHour: number;
  createdDate: string;
  modifiedDate: string;
}

interface PreferencesData {
  userPreferences: UserPreferences | null;
}

const Preferences = () => {
  const { data, loading, refetch } = useQuery<PreferencesData>(GET_USER_PREFERENCES);
  const [updatePreferences, { loading: updating }] = useMutation(UPDATE_USER_PREFERENCES);

  const [dayStartHour, setDayStartHour] = useState<number>(0);
  const [saveMessage, setSaveMessage] = useState<string>('');

  useEffect(() => {
    if (data?.userPreferences) {
      setDayStartHour(data.userPreferences.dayStartHour);
    }
  }, [data]);

  const handleSave = async () => {
    try {
      await updatePreferences({
        variables: { dayStartHour }
      });
      setSaveMessage('✨ Preferences saved successfully!');
      refetch();
      setTimeout(() => setSaveMessage(''), 3000);
    } catch (error) {
      console.error('Error saving preferences:', error);
      setSaveMessage('❌ Failed to save preferences');
      setTimeout(() => setSaveMessage(''), 3000);
    }
  };

  if (loading) return <div className="preferences-container">Loading preferences...</div>;

  const formatTime = (hour: number) => {
    if (hour === 0) return '12:00 AM (Midnight)';
    if (hour === 12) return '12:00 PM (Noon)';
    if (hour < 12) return `${hour}:00 AM`;
    return `${hour - 12}:00 PM`;
  };

  return (
    <div className="preferences-container">
      <h2>⚙️ Preferences</h2>

      <div className="preferences-card">
        <h3>Theme</h3>
        <p className="preference-description">
          Choose your preferred color theme for the app.
        </p>
        <ThemeSelector />
      </div>

      <div className="preferences-card">
        <h3>Day Start Time</h3>
        <p className="preference-description">
          Set when your "day" begins. Chores completed after this time will count for the current day,
          while chores completed before will count for the previous day. Perfect for night owls! 🦉
        </p>

        <div className="form-group">
          <label htmlFor="dayStartHour">
            Your day starts at: <strong>{formatTime(dayStartHour)}</strong>
          </label>
          <input
            type="range"
            id="dayStartHour"
            min="0"
            max="23"
            value={dayStartHour}
            onChange={(e) => setDayStartHour(parseInt(e.target.value))}
            className="time-slider"
          />
          <div className="time-labels">
            <span>Midnight</span>
            <span>Noon</span>
            <span>Midnight</span>
          </div>
        </div>

        <button
          onClick={handleSave}
          className="btn-primary"
          disabled={updating}
        >
          {updating ? 'Saving...' : 'Save Preferences'}
        </button>

        {saveMessage && (
          <div className={`save-message ${saveMessage.includes('✨') ? 'success' : 'error'}`}>
            {saveMessage}
          </div>
        )}
      </div>
    </div>
  );
};

export default Preferences;
