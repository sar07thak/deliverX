import React, { useState, useEffect } from 'react';
import { getMyRatings, getRatingSummary, getBehaviorIndex } from '../services/ratingService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

const RatingsPage = () => {
  const [ratings, setRatings] = useState([]);
  const [summary, setSummary] = useState(null);
  const [behaviorIndex, setBehaviorIndex] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  useEffect(() => {
    loadData();
  }, [page]);

  const loadData = async () => {
    try {
      setLoading(true);
      const [ratingsRes, summaryRes, behaviorRes] = await Promise.all([
        getMyRatings(page, 10),
        getRatingSummary(),
        getBehaviorIndex()
      ]);
      setRatings(ratingsRes.items || []);
      setTotalPages(ratingsRes.totalPages || 1);
      setSummary(summaryRes);
      setBehaviorIndex(behaviorRes);
    } catch (err) {
      setError(err.message || 'Failed to load ratings');
    } finally {
      setLoading(false);
    }
  };

  const renderStars = (score) => {
    return [...Array(5)].map((_, i) => (
      <span key={i} style={{ color: i < score ? '#ffc107' : '#e0e0e0', fontSize: '1.2em' }}>
        ★
      </span>
    ));
  };

  const getScoreColor = (score) => {
    if (score >= 80) return '#28a745';
    if (score >= 60) return '#ffc107';
    if (score >= 40) return '#fd7e14';
    return '#dc3545';
  };

  if (loading) return <LoadingSpinner />;

  return (
    <div style={{ padding: '20px', maxWidth: '1200px', margin: '0 auto' }}>
      <h1>My Ratings & Performance</h1>

      {error && <ErrorMessage message={error} />}

      {/* Summary Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '20px', marginBottom: '30px' }}>
        {/* Average Rating */}
        <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', textAlign: 'center' }}>
          <h3 style={{ margin: '0 0 10px', color: '#666' }}>Average Rating</h3>
          <div style={{ fontSize: '2.5em', fontWeight: 'bold', color: '#333' }}>
            {summary?.averageRating?.toFixed(1) || '0.0'}
          </div>
          <div>{renderStars(Math.round(summary?.averageRating || 0))}</div>
          <p style={{ color: '#999', margin: '5px 0 0' }}>{summary?.totalRatings || 0} ratings</p>
        </div>

        {/* Behavior Index */}
        <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', textAlign: 'center' }}>
          <h3 style={{ margin: '0 0 10px', color: '#666' }}>Behavior Score</h3>
          <div style={{
            fontSize: '2.5em',
            fontWeight: 'bold',
            color: getScoreColor(behaviorIndex?.behaviorScore || 0)
          }}>
            {behaviorIndex?.behaviorScore?.toFixed(0) || '0'}%
          </div>
          <div style={{ width: '100%', background: '#e0e0e0', borderRadius: '5px', height: '10px', marginTop: '10px' }}>
            <div style={{
              width: `${behaviorIndex?.behaviorScore || 0}%`,
              background: getScoreColor(behaviorIndex?.behaviorScore || 0),
              height: '100%',
              borderRadius: '5px',
              transition: 'width 0.3s'
            }} />
          </div>
        </div>

        {/* Completion Rate */}
        <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', textAlign: 'center' }}>
          <h3 style={{ margin: '0 0 10px', color: '#666' }}>Completion Rate</h3>
          <div style={{ fontSize: '2.5em', fontWeight: 'bold', color: '#17a2b8' }}>
            {behaviorIndex?.completionRate?.toFixed(0) || '0'}%
          </div>
        </div>

        {/* Punctuality Rate */}
        <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', textAlign: 'center' }}>
          <h3 style={{ margin: '0 0 10px', color: '#666' }}>Punctuality</h3>
          <div style={{ fontSize: '2.5em', fontWeight: 'bold', color: '#6f42c1' }}>
            {behaviorIndex?.punctualityRate?.toFixed(0) || '0'}%
          </div>
        </div>
      </div>

      {/* Recent Ratings */}
      <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
        <h2 style={{ marginTop: 0 }}>Recent Ratings</h2>

        {ratings.length === 0 ? (
          <p style={{ color: '#666', textAlign: 'center', padding: '40px' }}>
            No ratings yet. Complete deliveries to receive ratings.
          </p>
        ) : (
          <>
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                  <tr style={{ borderBottom: '2px solid #e0e0e0' }}>
                    <th style={{ padding: '12px', textAlign: 'left' }}>Date</th>
                    <th style={{ padding: '12px', textAlign: 'left' }}>From</th>
                    <th style={{ padding: '12px', textAlign: 'center' }}>Rating</th>
                    <th style={{ padding: '12px', textAlign: 'left' }}>Tags</th>
                    <th style={{ padding: '12px', textAlign: 'left' }}>Comment</th>
                  </tr>
                </thead>
                <tbody>
                  {ratings.map((rating) => (
                    <tr key={rating.id} style={{ borderBottom: '1px solid #e0e0e0' }}>
                      <td style={{ padding: '12px' }}>
                        {new Date(rating.createdAt).toLocaleDateString()}
                      </td>
                      <td style={{ padding: '12px' }}>
                        {rating.isAnonymous ? 'Anonymous' : rating.raterType}
                      </td>
                      <td style={{ padding: '12px', textAlign: 'center' }}>
                        {renderStars(rating.score)}
                      </td>
                      <td style={{ padding: '12px' }}>
                        {rating.tags && JSON.parse(rating.tags).map((tag, i) => (
                          <span key={i} style={{
                            background: '#e3f2fd',
                            color: '#1976d2',
                            padding: '2px 8px',
                            borderRadius: '12px',
                            marginRight: '5px',
                            fontSize: '0.85em'
                          }}>
                            {tag}
                          </span>
                        ))}
                      </td>
                      <td style={{ padding: '12px', color: '#666' }}>
                        {rating.comment || '-'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            <div style={{ display: 'flex', justifyContent: 'center', gap: '10px', marginTop: '20px' }}>
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                style={{
                  padding: '8px 16px',
                  border: '1px solid #ddd',
                  borderRadius: '5px',
                  background: page === 1 ? '#f5f5f5' : 'white',
                  cursor: page === 1 ? 'not-allowed' : 'pointer'
                }}
              >
                Previous
              </button>
              <span style={{ padding: '8px 16px' }}>
                Page {page} of {totalPages}
              </span>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                style={{
                  padding: '8px 16px',
                  border: '1px solid #ddd',
                  borderRadius: '5px',
                  background: page === totalPages ? '#f5f5f5' : 'white',
                  cursor: page === totalPages ? 'not-allowed' : 'pointer'
                }}
              >
                Next
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default RatingsPage;
