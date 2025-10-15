import { useState, useEffect } from 'react';
import { useMutation, useQuery } from '@apollo/client';
import { CREATE_CHORE, UPDATE_CHORE } from '../graphql/mutations';
import { GET_CHORE } from '../graphql/queries';
import { ChoreCategory } from '../types';
import { useNavigate, useParams } from 'react-router-dom';

const ChoreForm = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isEditing = Boolean(id);

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [location, setLocation] = useState('');
  const [category, setCategory] = useState<ChoreCategory>(ChoreCategory.Daily);
  const [deadline, setDeadline] = useState('');
  const [dayInterval, setDayInterval] = useState<number>(1);
  const [weekInterval, setWeekInterval] = useState<number>(1);
  const [selectedDaysOfWeek, setSelectedDaysOfWeek] = useState<string[]>([]);
  const [monthInterval, setMonthInterval] = useState<number>(1);
  const [dayOfMonth, setDayOfMonth] = useState<number>(1);
  const [hasSpecificMonthlyDay, setHasSpecificMonthlyDay] = useState<boolean>(false);
  const [endDate, setEndDate] = useState('');
  const [repeatInfinitely, setRepeatInfinitely] = useState<boolean>(true);

  const { data: choreData } = useQuery(GET_CHORE, {
    variables: { id: parseInt(id!) },
    skip: !isEditing,
  });

  const [createChore, { loading: creating }] = useMutation(CREATE_CHORE);
  const [updateChore, { loading: updating }] = useMutation(UPDATE_CHORE);

  useEffect(() => {
    if (choreData?.chore) {
      const chore = choreData.chore;
      setName(chore.name);
      setDescription(chore.description || '');
      setLocation(chore.location || '');
      setCategory(chore.category);
      setDeadline(chore.deadline ? chore.deadline.split('T')[0] : '');

      if (chore.recurrencePattern) {
        setDayInterval(chore.recurrencePattern.dayInterval || 1);
        setWeekInterval(chore.recurrencePattern.weekInterval || 1);
        setSelectedDaysOfWeek(chore.recurrencePattern.daysOfWeek || []);
        setMonthInterval(chore.recurrencePattern.monthInterval || 1);
        if (chore.recurrencePattern.dayOfMonth) {
          setDayOfMonth(chore.recurrencePattern.dayOfMonth);
          setHasSpecificMonthlyDay(true);
        }
        if (chore.recurrencePattern.endDate) {
          setEndDate(chore.recurrencePattern.endDate.split('T')[0]);
          setRepeatInfinitely(false);
        }
      }
    }
  }, [choreData]);

  const toggleDayOfWeek = (day: string) => {
    setSelectedDaysOfWeek((prev) =>
      prev.includes(day) ? prev.filter((d) => d !== day) : [...prev, day]
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const recurrencePattern =
      category !== ChoreCategory.Special
        ? {
            dayInterval: category === ChoreCategory.Daily ? dayInterval : null,
            weekInterval: category === ChoreCategory.Weekly ? weekInterval : null,
            daysOfWeek: null, // Weekly chores don't need specific days anymore!
            monthInterval: category === ChoreCategory.Monthly ? monthInterval : null,
            dayOfMonth: category === ChoreCategory.Monthly && hasSpecificMonthlyDay ? dayOfMonth : null,
            endDate: !repeatInfinitely && endDate ? new Date(endDate).toISOString() : null,
          }
        : null;

    const input = {
      name,
      description: description || null,
      location: location || null,
      category,
      recurrencePattern,
      deadline: deadline || null,
    };

    try {
      if (isEditing) {
        await updateChore({
          variables: { id: parseInt(id!), input },
        });
      } else {
        await createChore({
          variables: { input },
        });
      }
      navigate('/');
    } catch (error) {
      console.error('Error saving chore:', error);
      alert('Failed to save chore');
    }
  };

  const daysOfWeek = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

  return (
    <div className="chore-form-container">
      <h2>{isEditing ? 'Edit Chore' : 'Add New Chore'}</h2>

      <form onSubmit={handleSubmit} className="chore-form">
        <div className="form-group">
          <label htmlFor="name">Chore Name *</label>
          <input
            type="text"
            id="name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            placeholder="e.g., Sweep floors"
          />
        </div>

        <div className="form-group">
          <label htmlFor="description">Description</label>
          <textarea
            id="description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Add details about this chore..."
            rows={3}
          />
        </div>

        <div className="form-group">
          <label htmlFor="location">Location</label>
          <input
            type="text"
            id="location"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
            placeholder="e.g., Kitchen, Bathroom, Living Room"
            list="location-suggestions"
          />
          <datalist id="location-suggestions">
            <option value="Kitchen" />
            <option value="Bathroom" />
            <option value="Living Room" />
            <option value="Bedroom" />
            <option value="Garage" />
            <option value="Garden" />
            <option value="Office" />
          </datalist>
        </div>

        <div style={{ display: 'flex', gap: '1rem', alignItems: 'flex-start' }}>
          <div className="form-group" style={{ flex: 1 }}>
            <label htmlFor="category">Category *</label>
            <select
              id="category"
              value={category}
              onChange={(e) => setCategory(e.target.value as ChoreCategory)}
              required
            >
              <option value={ChoreCategory.Daily}>Daily</option>
              <option value={ChoreCategory.Weekly}>Weekly</option>
              <option value={ChoreCategory.Monthly}>Monthly</option>
              <option value={ChoreCategory.Special}>Special (One-time)</option>
            </select>
          </div>

          {category === ChoreCategory.Daily && (
            <div className="form-group" style={{ flex: 1 }}>
              <label htmlFor="dayInterval" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                Repeat every (days):
                <span style={{ position: 'relative', display: 'inline-flex', cursor: 'help' }} title="Examples: 1 = daily, 7 = weekly, 30 = monthly, 90 = quarterly">
                  <span style={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    width: '16px',
                    height: '16px',
                    borderRadius: '50%',
                    border: '1.5px solid var(--text-secondary)',
                    fontSize: '0.7rem',
                    color: 'var(--text-secondary)',
                    fontWeight: 'bold'
                  }}>i</span>
                </span>
              </label>
              <input
                type="number"
                id="dayInterval"
                value={dayInterval}
                onChange={(e) => setDayInterval(parseInt(e.target.value))}
                min="1"
                required
              />
            </div>
          )}

          {category === ChoreCategory.Weekly && (
            <div className="form-group" style={{ flex: 1 }}>
              <label htmlFor="weekInterval" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                Repeat every (weeks):
                <span style={{ position: 'relative', display: 'inline-flex', cursor: 'help' }} title="Examples: 1 = every week, 2 = every other week (bi-weekly), 3 = every 3 weeks 👏">
                  <span style={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    width: '16px',
                    height: '16px',
                    borderRadius: '50%',
                    border: '1.5px solid var(--text-secondary)',
                    fontSize: '0.7rem',
                    color: 'var(--text-secondary)',
                    fontWeight: 'bold'
                  }}>i</span>
                </span>
              </label>
              <input
                type="number"
                id="weekInterval"
                value={weekInterval}
                onChange={(e) => setWeekInterval(parseInt(e.target.value))}
                min="1"
                required
              />
            </div>
          )}
          {category === ChoreCategory.Monthly && (
            <div className="form-group" style={{ flex: 1 }}>
              <label htmlFor="monthInterval" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                Repeat every (months):
                <span style={{ position: 'relative', display: 'inline-flex', cursor: 'help' }} title="Examples: 1 = every month, 3 = every 3 months (quarterly), 6 = every 6 months (semi-annual) 👏">
                  <span style={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    width: '16px',
                    height: '16px',
                    borderRadius: '50%',
                    border: '1.5px solid var(--text-secondary)',
                    fontSize: '0.7rem',
                    color: 'var(--text-secondary)',
                    fontWeight: 'bold'
                  }}>i</span>
                </span>
              </label>
              <input
                type="number"
                id="monthInterval"
                value={monthInterval}
                onChange={(e) => setMonthInterval(parseInt(e.target.value))}
                min="1"
                required
              />
            </div>
          )}
        </div>

        {category === ChoreCategory.Monthly && (
          <div className="form-group">
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer', marginBottom: '0.5rem' }}>
              <input
                type="checkbox"
                checked={hasSpecificMonthlyDay}
                onChange={(e) => setHasSpecificMonthlyDay(e.target.checked)}
                style={{ width: 'auto', cursor: 'pointer' }}
              />
              <span>Set a specific due date each month (optional)</span>
            </label>

            {hasSpecificMonthlyDay && (
              <div style={{ marginTop: '0.5rem' }}>
                <label htmlFor="dayOfMonth" style={{ fontSize: '0.875rem' }}>Day of the month:</label>
                <input
                  type="number"
                  id="dayOfMonth"
                  value={dayOfMonth}
                  onChange={(e) => setDayOfMonth(parseInt(e.target.value))}
                  min="1"
                  max="31"
                  required
                  style={{ marginTop: '0.25rem' }}
                />
              </div>
            )}
          </div>
        )}

        {category !== ChoreCategory.Special && (
          <div className="form-group">
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer', marginBottom: '0.5rem' }}>
              <input
                type="checkbox"
                checked={repeatInfinitely}
                onChange={(e) => setRepeatInfinitely(e.target.checked)}
                style={{ width: 'auto', cursor: 'pointer' }}
              />
              <span>Repeat infinitely</span>
            </label>

            {!repeatInfinitely && (
              <div style={{ marginTop: '0.5rem' }}>
                <label htmlFor="endDate" style={{ fontSize: '0.875rem' }}>End date (when to stop repeating):</label>
                <input
                  type="date"
                  id="endDate"
                  value={endDate}
                  onChange={(e) => setEndDate(e.target.value)}
                  style={{ marginTop: '0.25rem' }}
                />
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.75rem', marginTop: '0.5rem' }}>
                  Perfect for seasonal chores like "Mow lawn" or "Water garden" 🌱
                </p>
              </div>
            )}
          </div>
        )}

        {category === ChoreCategory.Special && (
          <div className="form-group">
            <label htmlFor="deadline">Deadline:</label>
            <input
              type="date"
              id="deadline"
              value={deadline}
              onChange={(e) => setDeadline(e.target.value)}
            />
          </div>
        )}

        <div className="form-actions">
          <button
            type="button"
            onClick={() => navigate('/')}
            className="btn-secondary"
            disabled={creating || updating}
          >
            Cancel
          </button>
          <button type="submit" className="btn-primary" disabled={creating || updating}>
            {creating || updating ? 'Saving...' : isEditing ? 'Update Chore' : 'Create Chore'}
          </button>
        </div>
      </form>
    </div>
  );
};

export default ChoreForm;
