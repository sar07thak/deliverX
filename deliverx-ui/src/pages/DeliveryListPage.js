import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import deliveryService from '../services/deliveryService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

const DeliveryListPage = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [deliveries, setDeliveries] = useState([]);
  const [filter, setFilter] = useState('all');

  const isDP = user?.role === 'DP';

  useEffect(() => {
    fetchDeliveries();
  }, []);

  const fetchDeliveries = async () => {
    try {
      setLoading(true);
      setError('');
      const result = await deliveryService.getMyDeliveries();
      // Backend returns { deliveries: [], totalCount, page, pageSize }
      setDeliveries(result.deliveries || result.Deliveries || []);
    } catch (err) {
      console.error('Error fetching deliveries:', err);
      setError(err.message || 'Failed to fetch deliveries');
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status) => {
    const statusColors = {
      'CREATED': { bg: '#e3f2fd', color: '#1976d2' },
      'MATCHING': { bg: '#fff3e0', color: '#f57c00' },
      'ASSIGNED': { bg: '#e8f5e9', color: '#388e3c' },
      'ACCEPTED': { bg: '#e8f5e9', color: '#388e3c' },
      'PICKED_UP': { bg: '#e1f5fe', color: '#0288d1' },
      'IN_TRANSIT': { bg: '#f3e5f5', color: '#7b1fa2' },
      'DELIVERED': { bg: '#c8e6c9', color: '#2e7d32' },
      'CLOSED': { bg: '#f5f5f5', color: '#616161' },
      'CANCELLED': { bg: '#ffebee', color: '#c62828' },
      'UNASSIGNABLE': { bg: '#ffebee', color: '#c62828' }
    };

    const colors = statusColors[status] || { bg: '#e0e0e0', color: '#666' };

    return (
      <span style={{
        padding: '4px 10px',
        borderRadius: '12px',
        fontSize: '12px',
        fontWeight: '600',
        backgroundColor: colors.bg,
        color: colors.color
      }}>
        {status?.replace('_', ' ')}
      </span>
    );
  };

  const filteredDeliveries = deliveries.filter(d => {
    if (filter === 'all') return true;
    if (filter === 'active') return !['DELIVERED', 'CLOSED', 'CANCELLED'].includes(d.status);
    if (filter === 'completed') return d.status === 'DELIVERED' || d.status === 'CLOSED';
    if (filter === 'cancelled') return d.status === 'CANCELLED';
    return true;
  });

  const formatDate = (dateString) => {
    if (!dateString) return 'N/A';
    // Ensure UTC parsing by appending Z if not present
    const utcString = dateString.endsWith('Z') ? dateString : dateString + 'Z';
    const date = new Date(utcString);
    // Convert UTC to IST and format
    return date.toLocaleString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
      timeZone: 'Asia/Kolkata'
    });
  };

  if (loading) {
    return (
      <div className="container">
        <LoadingSpinner message="Loading deliveries..." />
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ maxWidth: '1000px', margin: '30px auto' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <div>
            <h1 style={{ color: '#2c3e50', margin: 0 }}>
              {isDP ? 'My Accepted Deliveries' : 'My Deliveries'}
            </h1>
            {isDP && (
              <p style={{ color: '#666', margin: '5px 0 0', fontSize: '14px' }}>
                Deliveries you have accepted and are working on
              </p>
            )}
          </div>
          {isDP ? (
            <button
              className="btn btn-primary"
              onClick={() => navigate('/deliveries/pending')}
            >
              View Available Jobs
            </button>
          ) : (
            <button
              className="btn btn-primary"
              onClick={() => navigate('/deliveries/create')}
            >
              + Create New Delivery
            </button>
          )}
        </div>

        {error && <ErrorMessage message={error} onClose={() => setError('')} />}

        {/* Filter Tabs */}
        <div style={{ display: 'flex', gap: '10px', marginBottom: '20px', flexWrap: 'wrap' }}>
          {['all', 'active', 'completed', 'cancelled'].map(f => (
            <button
              key={f}
              className={filter === f ? 'btn btn-primary' : 'btn btn-outline'}
              style={{ textTransform: 'capitalize' }}
              onClick={() => setFilter(f)}
            >
              {f} ({f === 'all' ? deliveries.length :
                f === 'active' ? deliveries.filter(d => !['DELIVERED', 'CLOSED', 'CANCELLED'].includes(d.status)).length :
                f === 'completed' ? deliveries.filter(d => d.status === 'DELIVERED' || d.status === 'CLOSED').length :
                deliveries.filter(d => d.status === 'CANCELLED').length})
            </button>
          ))}
        </div>

        {/* Deliveries List */}
        {filteredDeliveries.length === 0 ? (
          <div className="card" style={{ textAlign: 'center', padding: '50px' }}>
            <p style={{ fontSize: '48px', marginBottom: '10px' }}>{isDP ? '🚚' : '📦'}</p>
            <h3 style={{ color: '#666', marginBottom: '10px' }}>No Deliveries Found</h3>
            <p style={{ color: '#999', marginBottom: '20px' }}>
              {isDP
                ? (filter === 'all' ? 'You haven\'t accepted any deliveries yet.' : `No ${filter} deliveries.`)
                : (filter === 'all' ? 'You haven\'t created any deliveries yet.' : `No ${filter} deliveries.`)
              }
            </p>
            <button
              className="btn btn-primary"
              onClick={() => navigate(isDP ? '/deliveries/pending' : '/deliveries/create')}
            >
              {isDP ? 'View Available Jobs' : 'Create Your First Delivery'}
            </button>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
            {filteredDeliveries.map(delivery => (
              <div
                key={delivery.id}
                className="card"
                style={{ cursor: 'pointer', transition: 'transform 0.2s, box-shadow 0.2s' }}
                onClick={() => navigate(`/deliveries/${delivery.id}`)}
                onMouseEnter={e => {
                  e.currentTarget.style.transform = 'translateY(-2px)';
                  e.currentTarget.style.boxShadow = '0 4px 15px rgba(0,0,0,0.1)';
                }}
                onMouseLeave={e => {
                  e.currentTarget.style.transform = 'translateY(0)';
                  e.currentTarget.style.boxShadow = '';
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '15px' }}>
                  <div>
                    <p style={{ fontSize: '12px', color: '#999', marginBottom: '4px' }}>
                      #{delivery.id?.substring(0, 8)}...
                    </p>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                      {getStatusBadge(delivery.status)}
                      <span style={{ fontSize: '12px', color: '#666' }}>
                        {formatDate(delivery.createdAt)}
                      </span>
                    </div>
                  </div>
                  <div style={{ textAlign: 'right' }}>
                    <p style={{ fontSize: '18px', fontWeight: '600', color: '#667eea' }}>
                      Rs. {delivery.estimatedPrice?.toFixed(2) || '0.00'}
                    </p>
                    <p style={{ fontSize: '12px', color: '#666' }}>
                      {delivery.distanceKm?.toFixed(2) || '~'} km
                    </p>
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr auto 1fr', gap: '10px', alignItems: 'center' }}>
                  <div>
                    <p style={{ fontSize: '11px', color: '#667eea', fontWeight: '600', marginBottom: '2px' }}>PICKUP</p>
                    <p style={{ fontSize: '13px', color: '#333', marginBottom: '2px' }}>
                      {delivery.pickupContactName || 'Sender'}
                    </p>
                    <p style={{ fontSize: '12px', color: '#666', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {delivery.pickupAddress}
                    </p>
                  </div>
                  <div style={{ fontSize: '20px', color: '#ccc' }}>→</div>
                  <div>
                    <p style={{ fontSize: '11px', color: '#28a745', fontWeight: '600', marginBottom: '2px' }}>DROP</p>
                    <p style={{ fontSize: '13px', color: '#333', marginBottom: '2px' }}>
                      {delivery.dropContactName || 'Recipient'}
                    </p>
                    <p style={{ fontSize: '12px', color: '#666', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {delivery.dropAddress}
                    </p>
                  </div>
                </div>

                {delivery.assignedDPName && (
                  <div style={{ marginTop: '15px', paddingTop: '15px', borderTop: '1px solid #eee' }}>
                    <p style={{ fontSize: '12px', color: '#666' }}>
                      Delivery Partner: <strong>{delivery.assignedDPName}</strong>
                    </p>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}

        {/* Refresh Button */}
        <div style={{ textAlign: 'center', marginTop: '20px' }}>
          <button
            className="btn btn-outline"
            onClick={fetchDeliveries}
          >
            Refresh List
          </button>
        </div>
      </div>
    </div>
  );
};

export default DeliveryListPage;
