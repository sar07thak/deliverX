import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getComplaint, addComment, getCategories, getSeverityLevels } from '../services/complaintService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

const ComplaintDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [complaint, setComplaint] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [newComment, setNewComment] = useState('');
  const [submittingComment, setSubmittingComment] = useState(false);

  const categories = getCategories();
  const severities = getSeverityLevels();

  useEffect(() => {
    loadComplaint();
  }, [id]);

  const loadComplaint = async () => {
    try {
      setLoading(true);
      const data = await getComplaint(id);
      setComplaint(data);
    } catch (err) {
      setError(err.message || 'Failed to load complaint');
    } finally {
      setLoading(false);
    }
  };

  const handleAddComment = async () => {
    if (!newComment.trim()) return;

    try {
      setSubmittingComment(true);
      await addComment(id, newComment);
      setNewComment('');
      loadComplaint();
    } catch (err) {
      setError(err.message || 'Failed to add comment');
    } finally {
      setSubmittingComment(false);
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
        padding: '6px 16px',
        borderRadius: '20px',
        fontSize: '0.9em',
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
        padding: '4px 10px',
        borderRadius: '4px',
        fontSize: '0.85em',
        fontWeight: '600',
        color: 'white',
        backgroundColor: sev?.color || 'gray'
      }}>
        {severity}
      </span>
    );
  };

  if (loading) return <LoadingSpinner />;
  if (!complaint) return <ErrorMessage message="Complaint not found" />;

  return (
    <div style={{ padding: '20px', maxWidth: '900px', margin: '0 auto' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', marginBottom: '20px' }}>
        <button
          onClick={() => navigate('/complaints')}
          style={{
            background: 'none',
            border: 'none',
            fontSize: '1.5em',
            cursor: 'pointer',
            marginRight: '15px'
          }}
        >
          ←
        </button>
        <div style={{ flex: 1 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '5px' }}>
            <h1 style={{ margin: 0 }}>#{complaint.complaintNumber}</h1>
            {getSeverityBadge(complaint.severity)}
          </div>
          <p style={{ margin: 0, color: '#666' }}>
            Filed on {new Date(complaint.createdAt).toLocaleDateString()}
          </p>
        </div>
        {getStatusBadge(complaint.status)}
      </div>

      {error && <ErrorMessage message={error} onClose={() => setError('')} />}

      {/* Complaint Details */}
      <div style={{ background: 'white', padding: '25px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', marginBottom: '20px' }}>
        <h2 style={{ margin: '0 0 15px' }}>{complaint.subject}</h2>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '15px', marginBottom: '20px' }}>
          <div>
            <p style={{ margin: 0, color: '#666', fontSize: '0.85em' }}>Category</p>
            <p style={{ margin: '5px 0 0', fontWeight: '500' }}>
              {categories.find(c => c.value === complaint.category)?.label || complaint.category}
            </p>
          </div>
          {complaint.deliveryId && (
            <div>
              <p style={{ margin: 0, color: '#666', fontSize: '0.85em' }}>Related Delivery</p>
              <p style={{ margin: '5px 0 0', fontWeight: '500' }}>
                <a href={`/deliveries/${complaint.deliveryId}`} style={{ color: '#667eea' }}>
                  View Delivery
                </a>
              </p>
            </div>
          )}
          {complaint.assignedTo && (
            <div>
              <p style={{ margin: 0, color: '#666', fontSize: '0.85em' }}>Assigned To</p>
              <p style={{ margin: '5px 0 0', fontWeight: '500' }}>{complaint.assignedToName || 'Support Agent'}</p>
            </div>
          )}
          {complaint.resolvedAt && (
            <div>
              <p style={{ margin: 0, color: '#666', fontSize: '0.85em' }}>Resolved On</p>
              <p style={{ margin: '5px 0 0', fontWeight: '500' }}>
                {new Date(complaint.resolvedAt).toLocaleDateString()}
              </p>
            </div>
          )}
        </div>

        <div style={{ borderTop: '1px solid #e0e0e0', paddingTop: '15px' }}>
          <p style={{ margin: 0, color: '#666', fontSize: '0.85em' }}>Description</p>
          <p style={{ margin: '10px 0 0', lineHeight: '1.6', whiteSpace: 'pre-wrap' }}>
            {complaint.description}
          </p>
        </div>

        {complaint.resolution && (
          <div style={{ borderTop: '1px solid #e0e0e0', paddingTop: '15px', marginTop: '15px', background: '#f8f9fa', margin: '15px -25px -25px', padding: '20px 25px', borderRadius: '0 0 10px 10px' }}>
            <p style={{ margin: 0, color: '#28a745', fontWeight: '600' }}>Resolution</p>
            <p style={{ margin: '10px 0 0', lineHeight: '1.6' }}>{complaint.resolution}</p>
          </div>
        )}
      </div>

      {/* Comments Section */}
      <div style={{ background: 'white', padding: '25px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
        <h3 style={{ margin: '0 0 20px' }}>Comments & Updates</h3>

        {/* Comment List */}
        {complaint.comments && complaint.comments.length > 0 ? (
          <div style={{ marginBottom: '20px' }}>
            {complaint.comments.map((comment, index) => (
              <div key={index} style={{
                padding: '15px',
                background: comment.isStaff ? '#e3f2fd' : '#f8f9fa',
                borderRadius: '8px',
                marginBottom: '10px',
                borderLeft: comment.isStaff ? '3px solid #1976d2' : '3px solid #ddd'
              }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
                  <span style={{ fontWeight: '500', color: comment.isStaff ? '#1976d2' : '#333' }}>
                    {comment.isStaff ? 'Support Team' : 'You'}
                  </span>
                  <span style={{ color: '#999', fontSize: '0.85em' }}>
                    {new Date(comment.createdAt).toLocaleString()}
                  </span>
                </div>
                <p style={{ margin: 0, lineHeight: '1.5' }}>{comment.message}</p>
              </div>
            ))}
          </div>
        ) : (
          <p style={{ color: '#666', marginBottom: '20px' }}>No comments yet.</p>
        )}

        {/* Add Comment */}
        {complaint.status !== 'CLOSED' && complaint.status !== 'REJECTED' && (
          <div>
            <textarea
              value={newComment}
              onChange={(e) => setNewComment(e.target.value)}
              placeholder="Add a comment or provide additional information..."
              rows={3}
              style={{
                width: '100%',
                padding: '12px',
                borderRadius: '5px',
                border: '1px solid #ddd',
                resize: 'vertical',
                boxSizing: 'border-box',
                marginBottom: '10px'
              }}
            />
            <button
              onClick={handleAddComment}
              disabled={submittingComment || !newComment.trim()}
              style={{
                padding: '10px 20px',
                border: 'none',
                borderRadius: '5px',
                background: submittingComment || !newComment.trim() ? '#ccc' : '#667eea',
                color: 'white',
                cursor: submittingComment || !newComment.trim() ? 'not-allowed' : 'pointer',
                fontWeight: '500'
              }}
            >
              {submittingComment ? 'Sending...' : 'Add Comment'}
            </button>
          </div>
        )}

        {(complaint.status === 'CLOSED' || complaint.status === 'REJECTED') && (
          <p style={{ color: '#666', fontStyle: 'italic', textAlign: 'center', padding: '20px', background: '#f8f9fa', borderRadius: '5px' }}>
            This complaint is {complaint.status.toLowerCase()}. No further comments can be added.
          </p>
        )}
      </div>
    </div>
  );
};

export default ComplaintDetailPage;
