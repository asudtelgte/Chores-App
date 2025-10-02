import { useQuery } from '@apollo/client';
import { GET_CHORES } from '../graphql/queries';
import type { Chore } from '../types';
import { ChoreCategory } from '../types';
import { useState } from 'react';
import ChoreItem from './ChoreItem';

interface ChoresData {
  chores: Chore[];
}

const ChoreList = () => {
  const { loading, error, data, refetch } = useQuery<ChoresData>(GET_CHORES);
  const [filterCategory, setFilterCategory] = useState<ChoreCategory | 'ALL'>('ALL');

  if (loading) return <div>Loading chores...</div>;
  if (error) return <div>Error loading chores: {error.message}</div>;

  const filteredChores = data?.chores.filter((chore) => {
    if (filterCategory === 'ALL') return chore.isActive;
    return chore.isActive && chore.category === filterCategory;
  }) || [];

  const categoryCounts = {
    ALL: data?.chores.filter(c => c.isActive).length || 0,
    DAILY: data?.chores.filter(c => c.isActive && c.category === ChoreCategory.Daily).length || 0,
    WEEKLY: data?.chores.filter(c => c.isActive && c.category === ChoreCategory.Weekly).length || 0,
    MONTHLY: data?.chores.filter(c => c.isActive && c.category === ChoreCategory.Monthly).length || 0,
    SPECIAL: data?.chores.filter(c => c.isActive && c.category === ChoreCategory.Special).length || 0,
  };

  return (
    <div className="chore-list">
      <div className="chore-list-header">
        <h2>My Chores</h2>
        <button onClick={() => window.location.href = '/chores/new'} className="btn-primary">
          + Add Chore
        </button>
      </div>

      <div className="filter-tabs">
        <button
          className={filterCategory === 'ALL' ? 'tab active' : 'tab'}
          onClick={() => setFilterCategory('ALL')}
        >
          All ({categoryCounts.ALL})
        </button>
        <button
          className={filterCategory === ChoreCategory.Daily ? 'tab active' : 'tab'}
          onClick={() => setFilterCategory(ChoreCategory.Daily)}
        >
          Daily ({categoryCounts.DAILY})
        </button>
        <button
          className={filterCategory === ChoreCategory.Weekly ? 'tab active' : 'tab'}
          onClick={() => setFilterCategory(ChoreCategory.Weekly)}
        >
          Weekly ({categoryCounts.WEEKLY})
        </button>
        <button
          className={filterCategory === ChoreCategory.Monthly ? 'tab active' : 'tab'}
          onClick={() => setFilterCategory(ChoreCategory.Monthly)}
        >
          Monthly ({categoryCounts.MONTHLY})
        </button>
        <button
          className={filterCategory === ChoreCategory.Special ? 'tab active' : 'tab'}
          onClick={() => setFilterCategory(ChoreCategory.Special)}
        >
          Special ({categoryCounts.SPECIAL})
        </button>
      </div>

      <div className="chores-list">
        {filteredChores.length === 0 ? (
          <div className="empty-state">
            <p>No chores found. Add your first chore to get started!</p>
          </div>
        ) : (
          filteredChores.map((chore) => (
            <ChoreItem key={chore.id} chore={chore} onUpdate={refetch} />
          ))
        )}
      </div>
    </div>
  );
};

export default ChoreList;
