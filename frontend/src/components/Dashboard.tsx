import { useQuery } from '@apollo/client';
import { GET_CHORES, GET_COMPLETION_HISTORY, GET_CHORE_SKIP_ANALYTICS } from '../graphql/queries';
import type { Chore, ChoreCompletion } from '../types';
import { ChoreCategory } from '../types';
import { LineChart, Line, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

interface ChoresData {
  chores: Chore[];
}

interface CompletionHistoryData {
  completionHistory: ChoreCompletion[];
}

interface SkipAnalytics {
  choreId: number;
  choreName: string;
  totalSkips: number;
  totalCompletions: number;
  skipRate: number;
  averageNecessityAfterSkip: number | null;
  averageNecessityOnTime: number | null;
  necessityDifference: number | null;
  frequencyRecommendation: string | null;
}

interface SkipAnalyticsData {
  choreSkipAnalytics: SkipAnalytics[];
}

const Dashboard = () => {
  const { data: choresData, loading: choresLoading } = useQuery<ChoresData>(GET_CHORES);
  const { data: historyData, loading: historyLoading } = useQuery<CompletionHistoryData>(GET_COMPLETION_HISTORY);
  const { data: skipData, loading: skipLoading } = useQuery<SkipAnalyticsData>(GET_CHORE_SKIP_ANALYTICS);

  if (choresLoading || historyLoading || skipLoading) return <div>Loading dashboard...</div>;

  const chores = choresData?.chores || [];
  const completions = historyData?.completionHistory || [];

  // Calculate stats
  const totalChores = chores.filter(c => c.isActive).length;
  const totalCompletions = completions.length;
  const avgNecessityRating = completions.length > 0
    ? (completions.reduce((sum, c) => sum + c.necessityRating, 0) / completions.length).toFixed(2)
    : '0';

  // Completions by location
  const completionsByLocation: { [key: string]: number } = {};

  completions.forEach((completion) => {
    const location = completion.chore?.location || 'No Location';
    completionsByLocation[location] = (completionsByLocation[location] || 0) + 1;
  });

  const locationChartData = Object.entries(completionsByLocation)
    .map(([location, count]) => ({
      location,
      completions: count,
    }))
    .sort((a, b) => b.completions - a.completions);

  // Completions over time (last 30 days)
  const last30Days = Array.from({ length: 30 }, (_, i) => {
    const date = new Date();
    date.setDate(date.getDate() - (29 - i));
    return date.toISOString().split('T')[0];
  });

  const completionsByDate: { [key: string]: number } = {};
  completions.forEach((completion) => {
    const date = completion.completionDate.split('T')[0];
    completionsByDate[date] = (completionsByDate[date] || 0) + 1;
  });

  const timelineChartData = last30Days.map((date) => ({
    date: new Date(date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
    completions: completionsByDate[date] || 0,
  }));

  // Top chores by necessity rating
  const choreRatings = chores
    .filter(c => c.completions.length > 0)
    .map(chore => ({
      name: chore.name,
      avgRating: (chore.completions.reduce((sum, c) => sum + c.necessityRating, 0) / chore.completions.length).toFixed(1),
      completions: chore.completions.length,
    }))
    .sort((a, b) => parseFloat(b.avgRating) - parseFloat(a.avgRating))
    .slice(0, 5);

  return (
    <div className="dashboard">
      <h2>Dashboard</h2>

      <div className="stats-grid">
        <div className="stat-card">
          <h3>Total Active Chores</h3>
          <p className="stat-number">{totalChores}</p>
        </div>
        <div className="stat-card">
          <h3>Total Completions</h3>
          <p className="stat-number">{totalCompletions}</p>
        </div>
        <div className="stat-card">
          <h3>Avg Necessity Rating</h3>
          <p className="stat-number">{avgNecessityRating} / 5</p>
        </div>
        <div className="stat-card">
          <h3>Completion Rate</h3>
          <p className="stat-number">
            {totalChores > 0 ? ((totalCompletions / totalChores).toFixed(1)) : '0'}
          </p>
        </div>
      </div>

      <div className="charts-grid">
        <div className="chart-card">
          <h3>Completions Over Time (Last 30 Days)</h3>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={timelineChartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="date" />
              <YAxis />
              <Tooltip />
              <Legend />
              <Line type="monotone" dataKey="completions" stroke="#8884d8" strokeWidth={2} />
            </LineChart>
          </ResponsiveContainer>
        </div>

        <div className="chart-card">
          <h3>Completions by Location</h3>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={locationChartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="location" />
              <YAxis />
              <Tooltip />
              <Legend />
              <Bar dataKey="completions" fill="#82ca9d" />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="top-chores-card">
        <h3>Top Chores by Necessity Rating</h3>
        {choreRatings.length === 0 ? (
          <p className="empty-message">Complete some chores to see ratings!</p>
        ) : (
          <div className="top-chores-list">
            {choreRatings.map((chore, index) => (
              <div key={index} className="top-chore-item">
                <span className="rank">#{index + 1}</span>
                <span className="chore-name">{chore.name}</span>
                <span className="rating">⭐ {chore.avgRating}</span>
                <span className="completions">({chore.completions} times)</span>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="skip-analytics-section">
        <h3>📊 Skip Analytics & Frequency Insights</h3>
        {skipData?.choreSkipAnalytics && skipData.choreSkipAnalytics.length > 0 ? (
          <div className="skip-analytics-grid">
            {skipData.choreSkipAnalytics.map((analytics) => (
              <div key={analytics.choreId} className="skip-analytics-card">
                <h4>{analytics.choreName}</h4>
                <div className="analytics-stats">
                  <div className="stat-row">
                    <span className="stat-label">Skip Rate:</span>
                    <span className="stat-value">{(analytics.skipRate * 100).toFixed(1)}%</span>
                  </div>
                  <div className="stat-row">
                    <span className="stat-label">Total Skips:</span>
                    <span className="stat-value">{analytics.totalSkips}</span>
                  </div>
                  <div className="stat-row">
                    <span className="stat-label">Total Completions:</span>
                    <span className="stat-value">{analytics.totalCompletions}</span>
                  </div>
                  {analytics.averageNecessityAfterSkip !== null && (
                    <div className="stat-row">
                      <span className="stat-label">Avg Necessity (After Skip):</span>
                      <span className="stat-value">⭐ {analytics.averageNecessityAfterSkip.toFixed(1)}</span>
                    </div>
                  )}
                  {analytics.averageNecessityOnTime !== null && (
                    <div className="stat-row">
                      <span className="stat-label">Avg Necessity (On Time):</span>
                      <span className="stat-value">⭐ {analytics.averageNecessityOnTime.toFixed(1)}</span>
                    </div>
                  )}
                  {analytics.necessityDifference !== null && (
                    <div className="stat-row">
                      <span className="stat-label">Necessity Difference:</span>
                      <span
                        className="stat-value"
                        style={{
                          color: analytics.necessityDifference < -0.5 ? '#ff6b6b' :
                                 analytics.necessityDifference > 0.5 ? '#51cf66' : '#868e96'
                        }}
                      >
                        {analytics.necessityDifference > 0 ? '+' : ''}
                        {analytics.necessityDifference.toFixed(1)}
                      </span>
                    </div>
                  )}
                </div>
                {analytics.frequencyRecommendation && (
                  <div className="recommendation">
                    <strong>💡 Recommendation:</strong>
                    <p>{analytics.frequencyRecommendation}</p>
                  </div>
                )}
              </div>
            ))}
          </div>
        ) : (
          <p className="empty-message">
            Complete some chores to see skip analytics! Skipped chores and their necessity ratings will help you optimize your chore frequency.
          </p>
        )}
      </div>
    </div>
  );
};

export default Dashboard;
