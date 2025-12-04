import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getMyComplaints, getCategories, getSeverityLevels } from '../services/complaintService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

const ComplaintsPage = () => {
  const navigate = useNavigate();
  const [complaints, setComplaints] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  const categories = getCategories();
  const severities = getSeverityLevels();

  useEffect(() => {
    loadComplaints();
  }, [page, filter]);

  const loadComplaints = async () => {
    try {
      setLoading(true);
      const data = await getMyComplaints(page, 10, filter || null);
      setComplaints(data.items || []);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      setError(err.message || 'Failed to load complaints');
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status) => {
    const styles = {
      OPEN: { bg: '#fff3cd', color: '#856404' },
      ASSIGNED: { bg: '#cce5ff', color: '#004085' },
      IN_PROGRESS: { bg: '#d4edda', color: '#155724' },
      RESOLVED: { bg: '#d1ecf1', color: '#0c5460' },
      CLOSED: { bg: '#f5f5f5', color: '#666' },
      REJECTED: { bg: '#f8d7da', color: '#721c24' }
    };
    const style = styles[status] || styles.OPEN;
    return (
      <span style={{
        padding: '4px 12px',
        borderRadius: '20px',
        fontSize: '0.85em',
        fontWeight: '500',
        backgroundColor: style.bg,
        color: style.color
      }}>
        {status}
      </span>
    );
  };

  const getSeverityBadge = (severity) => {
    const sev = severities.find(s => s.value === severity);
    return (
      <span style={{
        padding: '4px 8px',
        borderRadius: '4px',
        fontSize: '0.8em',
        fontWeight: '600',
        color: 'white',
        backgroundColor: sev?.color || 'gray'
      }}>
        {severity}
      </span>
    );
  };

  if (loading && complaints.length === 0) return <LoadingSpinner />;

  return (
    <div style={{ padding: '20px', maxWidth: '1200px', margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
        <h1>My Complaints</h1>
        <button
          onClick={() => navigate('/complaints/new')}
          style={{
            padding: '10px 20px',
            backgroundColor: '#dc3545',
            color: 'white',
            border: 'none',
            borderRadius: '5px',
            cursor: 'pointer',
            fontWeight: '500'
          }}
        >
          + File New Complaint
        </button>
      </div>

      {error && <ErrorMessage message={error} />}

      {/* Filters */}
      <div style={{ marginBottom: '20px', display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
        <select
          value={filter}
          onChange={(e) => { setFilter(e.target.value); setPage(1); }}
          style={{
            padding: '10px 15px',
            borderRadius: '5px',
            border: '1px solid #ddd',
            minWidth: '150px'
          }}
        >
          <option value="">All Status</option>
          <option value="OPEN">Open</option>
          <option value="ASSIGNED">Assigned</option>
          <option value="IN_PROGRESS">In Progress</option>
          <option value="RESOLVED">Resolved</option>
          <option value="CLOSED">Closed</option>
        </select>
      </div>

      {/* Complaints List */}
      <div style={{ background: 'white', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
        {complaints.length === 0 ? (
          <p style={{ color: '#666', textAlign: 'center', padding: '60px 20px' }}>
            No complaints found. If you have any issues with a delivery, click "File New Complaint" above.
          </p>
        ) : (
          <>
            {complaints.map((complaint) => (
              <div
                key={complaint.id}
                onClick={() => navigate(`/complaints/${complaint.id}`)}
                style={{
                  padding: '20px',
                  borderBottom: '1px solid #e0e0e0',
                  cursor: 'pointer',
                  transition: 'background 0.2s'
                }}
                onMouseEnter={(e) => e.target.style.background = '#f8f9fa'}
                onMouseLeave={(e) => e.target.style.background = 'white'}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '10px' }}>
                  <div>
                    <span style={{ fontWeight: '600', marginRight: '10px' }}>
                      #{complaint.complaintNumber}
                    </span>
                    {getSeverityBadge(complaint.severity)}
                  </div>
                  {getStatusBadge(complaint.status)}
                </div>

                <h3 style={{ margin: '0 0 10px', color: '#333' }}>{complaint.subject}</h3>

                <div style={{ display: 'flex', gap: '20px', color: '#666', fontSize: '0.9em' }}>
                  <span>
                    <strong>Category:</strong> {categories.find(c => c.value === complaint.category)?.label || complaint.category}
                  </span>
                  <span>
                    <strong>Filed:</strong> {new Date(complaint.createdAt).toLocaleDateString()}
                  </span>
                  {complaint.resolvedAt && (
                    <span>
                      <strong>Resolved:</strong> {new Date(complaint.resolvedAt).toLocaleDateString()}
                    </span>
                  )}
                </div>

                <p style={{ margin: '10px 0 0', color: '#666', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {complaint.description}
                </p>
              </div>
            ))}

            {/* Pagination */}
            <div style={{ display: 'flex', justifyContent: 'center', gap: '10px', padding: '20px' }}>
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
              <span style={{ padding: '8px 16px' }}>Page {page} of {totalPages}</span>
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

export default ComplaintsPage;
