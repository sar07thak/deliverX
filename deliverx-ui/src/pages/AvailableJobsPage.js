import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import deliveryService from '../services/deliveryService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

/**
 * Available Jobs Page for Delivery Partners
 * Shows deliveries that have been matched to the DP and are waiting for acceptance
 */
const AvailableJobsPage = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [deliveries, setDeliveries] = useState([]);

  useEffect(() => {
    fetchPendingDeliveries();
  }, []);

  const fetchPendingDeliveries = async () => {
    try {
      setLoading(true);
      setError('');
      const result = await deliveryService.getPendingDeliveries();
      // Backend returns { deliveries: [], totalCount, page, pageSize }
      setDeliveries(result.deliveries || result.Deliveries || []);
    } catch (err) {
      console.error('Error fetching pending deliveries:', err);
      setError(err.message || 'Failed to fetch available jobs');
    } finally {
      setLoading(false);
    }
  };

  const handleAccept = async (deliveryId) => {
    try {
      setActionLoading(true);
      setError('');
      await deliveryService.acceptDelivery(deliveryId);
      setSuccess('Delivery accepted! Redirecting to details...');
      setTimeout(() => {
        navigate(`/deliveries/${deliveryId}`);
      }, 1500);
    } catch (err) {
      console.error('Error accepting delivery:', err);
      setError(err.message || 'Failed to accept delivery');
    } finally {
      setActionLoading(false);
    }
  };

  const handleReject = async (deliveryId) => {
    const reason = window.prompt('Please provide a reason for rejecting this job:');
    if (!reason) return;

    try {
      setActionLoading(true);
      setError('');
      await deliveryService.rejectDelivery(deliveryId, reason);
      setSuccess('Job rejected. Looking for other opportunities...');
      // Remove from list
      setDeliveries(prev => prev.filter(d => d.id !== deliveryId));
      setTimeout(() => setSuccess(''), 3000);
    } catch (err) {
      console.error('Error rejecting delivery:', err);
      setError(err.message || 'Failed to reject delivery');
    } finally {
      setActionLoading(false);
    }
  };

  const formatDate = (dateString) => {
    if (!dateString) return 'N/A';
    const utcString = dateString.endsWith('Z') ? dateString : dateString + 'Z';
    const date = new Date(utcString);
    return date.toLocaleString('en-IN', {
      day: '2-digit',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
      timeZone: 'Asia/Kolkata'
    });
  };

  if (loading) {
    return (
      <div className="container">
        <LoadingSpinner message="Loading available jobs..." />
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ maxWidth: '900px', margin: '30px auto' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <div>
            <h1 style={{ color: '#2c3e50', margin: 0 }}>Available Jobs</h1>
            <p style={{ color: '#666', marginTop: '5px' }}>
              Deliveries matched to you. Accept to start earning!
            </p>
          </div>
          <button className="btn btn-outline" onClick={fetchPendingDeliveries}>
            Refresh
          </button>
        </div>

        {error && <ErrorMessage message={error} onClose={() => setError('')} />}
        {success && (
          <div className="alert alert-success" style={{ marginBottom: '20px' }}>
            {success}
          </div>
        )}

        {deliveries.length === 0 ? (
          <div className="card" style={{ textAlign: 'center', padding: '50px' }}>
            <p style={{ fontSize: '48px', marginBottom: '10px' }}>🔍</p>
            <h3 style={{ color: '#666', marginBottom: '10px' }}>No Available Jobs</h3>
            <p style={{ color: '#999', marginBottom: '20px' }}>
              No deliveries are currently matched to you. Check back soon!
            </p>
            <p style={{ color: '#888', fontSize: '14px' }}>
              Make sure your service area is configured correctly to receive delivery requests.
            </p>
            <button
              className="btn btn-secondary"
              style={{ marginTop: '15px' }}
              onClick={() => navigate('/service-area')}
            >
              Check Service Area
            </button>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
            {deliveries.map(delivery => (
              <div key={delivery.id} className="card">
                {/* Header */}
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '15px' }}>
                  <div>
                    <p style={{ fontSize: '12px', color: '#999', marginBottom: '4px' }}>
                      Job #{delivery.id?.substring(0, 8)}...
                    </p>
                    <span style={{
                      padding: '4px 10px',
                      borderRadius: '12px',
                      fontSize: '12px',
                      fontWeight: '600',
                      backgroundColor: delivery.priority === 'URGENT' ? '#ffebee' : '#fff3e0',
                      color: delivery.priority === 'URGENT' ? '#c62828' : '#f57c00'
                    }}>
                      {delivery.priority || 'NORMAL'}
                    </span>
                  </div>
                  <div style={{ textAlign: 'right' }}>
                    <p style={{ fontSize: '24px', fontWeight: '700', color: '#28a745', marginBottom: '2px' }}>
                      Rs. {delivery.estimatedPrice?.toFixed(0) || '0'}
                    </p>
                    <p style={{ fontSize: '12px', color: '#666' }}>
                      {delivery.distanceKm?.toFixed(1) || '~'} km
                    </p>
                  </div>
                </div>

                {/* Route Info */}
                <div style={{ display: 'grid', gridTemplateColumns: '1fr auto 1fr', gap: '15px', alignItems: 'center', marginBottom: '15px' }}>
                  <div>
                    <p style={{ fontSize: '11px', color: '#667eea', fontWeight: '600', marginBottom: '4px' }}>PICKUP</p>
                    <p style={{ fontSize: '14px', color: '#333', fontWeight: '500' }}>
                      {delivery.pickupContactName || 'Sender'}
                    </p>
                    <p style={{ fontSize: '12px', color: '#666' }}>
                      {delivery.pickupAddress}
                    </p>
                  </div>
                  <div style={{ fontSize: '24px', color: '#4CAF50' }}>→</div>
                  <div>
                    <p style={{ fontSize: '11px', color: '#28a745', fontWeight: '600', marginBottom: '4px' }}>DROP</p>
                    <p style={{ fontSize: '14px', color: '#333', fontWeight: '500' }}>
                      {delivery.dropContactName || 'Recipient'}
                    </p>
                    <p style={{ fontSize: '12px', color: '#666' }}>
                      {delivery.dropAddress}
                    </p>
                  </div>
                </div>

                {/* Timestamp */}
                <p style={{ fontSize: '12px', color: '#999', marginBottom: '15px' }}>
                  Posted: {formatDate(delivery.createdAt)}
                </p>

                {/* Action Buttons */}
                <div style={{ display: 'flex', gap: '10px', paddingTop: '15px', borderTop: '1px solid #eee' }}>
                  <button
                    className="btn btn-primary"
                    style={{ flex: 2 }}
                    onClick={() => handleAccept(delivery.id)}
                    disabled={actionLoading}
                  >
                    {actionLoading ? 'Processing...' : 'Accept Job'}
                  </button>
                  <button
                    className="btn btn-outline"
                    style={{ flex: 1 }}
                    onClick={() => navigate(`/deliveries/${delivery.id}`)}
                    disabled={actionLoading}
                  >
                    View Details
                  </button>
                  <button
                    className="btn btn-danger"
                    style={{ flex: 1 }}
                    onClick={() => handleReject(delivery.id)}
                    disabled={actionLoading}
                  >
                    Reject
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Tips Section */}
        <div className="card" style={{ marginTop: '30px', backgroundColor: '#f8f9fa' }}>
          <h3 style={{ fontSize: '16px', color: '#2c3e50', marginBottom: '10px' }}>Tips for Success</h3>
          <ul style={{ margin: 0, paddingLeft: '20px', color: '#666', fontSize: '14px' }}>
            <li>Accept jobs quickly - others may accept before you!</li>
            <li>Check the route before accepting to plan your path</li>
            <li>Maintain high ratings by being professional and punctual</li>
            <li>Report any issues immediately through the app</li>
          </ul>
        </div>
      </div>
    </div>
  );
};

export default AvailableJobsPage;
