import { useState } from 'react';
import { useMutation } from '@apollo/client';
import type { Chore } from '../types';
import { DELETE_CHORE } from '../graphql/mutations';
import CompleteChoreModal from './CompleteChoreModal';

interface ChoreItemProps {
  chore: Chore;
  onUpdate: () => void;
}

const ChoreItem = ({ chore, onUpdate }: ChoreItemProps) => {
  const [showCompleteModal, setShowCompleteModal] = useState(false);
  const [deleteChore] = useMutation(DELETE_CHORE);

  const lastCompletion = chore.completions.length > 0
    ? chore.completions.reduce((latest, current) =>
        new Date(current.completionDate) > new Date(latest.completionDate) ? current : latest
      )
    : null;

  const avgRating = chore.completions.length > 0
    ? (chore.completions.reduce((sum, c) => sum + c.necessityRating, 0) / chore.completions.length).toFixed(1)
    : 'N/A';

  const handleDelete = async () => {
    if (window.confirm(`Are you sure you want to delete "${chore.name}"?`)) {
      try {
        await deleteChore({ variables: { id: chore.id } });
        onUpdate();
      } catch (error) {
        console.error('Error deleting chore:', error);
        alert('Failed to delete chore');
      }
    }
  };

  const handleCompleteSuccess = () => {
    setShowCompleteModal(false);
    onUpdate();
  };

  return (
    <>
      <div className="chore-row">
        <div className="chore-row-main">
          <button
            onClick={() => setShowCompleteModal(true)}
            className={`complete-checkbox ${chore.isCompletedForPeriod ? 'checked' : ''}`}
            title={chore.isCompletedForPeriod ? "Already completed for this period" : "Mark as complete"}
          >
            ✓
          </button>

          <div className="chore-row-content">
            <div className="chore-row-header">
              <h3 className="chore-row-title">{chore.name}</h3>
              <span className={`category-badge ${chore.category.toLowerCase()}`}>
                {chore.category}
              </span>
            </div>

            {chore.description && (
              <p className="chore-row-description">{chore.description}</p>
            )}

            <div className="chore-row-meta">
              {chore.location && (
                <span className="meta-item">📍 {chore.location}</span>
              )}
              <span className="meta-item">✓ {chore.completions.length} times</span>
              <span className="meta-item">⭐ {avgRating}</span>
              {lastCompletion && (
                <span className="meta-item">
                  Last: {new Date(lastCompletion.completionDate).toLocaleDateString()}
                </span>
              )}
              {chore.deadline && (
                <span className="meta-item meta-deadline">
                  Due: {new Date(chore.deadline).toLocaleDateString()}
                </span>
              )}
            </div>
          </div>
        </div>

        <div className="chore-row-actions">
          <button
            onClick={() => window.location.href = `/chores/${chore.id}/edit`}
            className="btn-icon"
            title="Edit"
          >
            ✏️
          </button>
          <button
            onClick={handleDelete}
            className="btn-icon btn-icon-danger"
            title="Delete"
          >
            🗑️
          </button>
        </div>
      </div>

      {showCompleteModal && (
        <CompleteChoreModal
          chore={chore}
          onClose={() => setShowCompleteModal(false)}
          onSuccess={handleCompleteSuccess}
        />
      )}
    </>
  );
};

export default ChoreItem;
