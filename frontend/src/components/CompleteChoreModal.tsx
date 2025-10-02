import { useState } from 'react';
import { useMutation } from '@apollo/client';
import { COMPLETE_CHORE } from '../graphql/mutations';
import type { Chore } from '../types';

interface CompleteChoreModalProps {
  chore: Chore;
  onClose: () => void;
  onSuccess: () => void;
}

const CompleteChoreModal = ({ chore, onClose, onSuccess }: CompleteChoreModalProps) => {
  const [rating, setRating] = useState<number>(3);
  const [notes, setNotes] = useState('');
  const [completeChore, { loading }] = useMutation(COMPLETE_CHORE);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      await completeChore({
        variables: {
          input: {
            choreId: chore.id,
            necessityRating: rating,
            notes: notes || null,
          },
        },
      });
      onSuccess();
    } catch (error) {
      console.error('Error completing chore:', error);
      alert('Failed to complete chore');
    }
  };

  const ratingLabels = [
    '1 - Unnecessary',
    '2 - Could wait',
    '3 - Needed',
    '4 - Quite necessary',
    '5 - Critical',
  ];

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <h2>Complete Chore</h2>
        <p className="modal-subtitle">How necessary was this chore?</p>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Chore:</label>
            <div className="chore-name">{chore.name}</div>
          </div>

          <div className="form-group">
            <label htmlFor="rating">Necessity Rating:</label>
            <div className="rating-selector">
              {[1, 2, 3, 4, 5].map((value) => (
                <button
                  key={value}
                  type="button"
                  className={`rating-button ${rating === value ? 'selected' : ''}`}
                  onClick={() => setRating(value)}
                >
                  {value}
                </button>
              ))}
            </div>
            <p className="rating-label">{ratingLabels[rating - 1]}</p>
          </div>

          <div className="form-group">
            <label htmlFor="notes">Notes (optional):</label>
            <textarea
              id="notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Add any observations about this chore..."
              rows={3}
            />
          </div>

          <div className="modal-actions">
            <button type="button" onClick={onClose} className="btn-secondary" disabled={loading}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={loading}>
              {loading ? 'Saving...' : 'Complete Chore'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default CompleteChoreModal;
