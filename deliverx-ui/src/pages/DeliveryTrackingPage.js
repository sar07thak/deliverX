import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import deliveryService from '../services/deliveryService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

const DeliveryTrackingPage = () => {
  const { id } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [delivery, setDelivery] = useState(null);
  const [stateInfo, setStateInfo] = useState(null);
  const [pod, setPod] = useState(null);

  // OTP state
  const [otpSent, setOtpSent] = useState(false);
  const [otpVerified, setOtpVerified] = useState(false);
  const [otp, setOtp] = useState('');

  // Action modals
  const [showPickupModal, setShowPickupModal] = useState(false);
  const [showDeliverModal, setShowDeliverModal] = useState(false);

  // Form data for actions
  const [pickupData, setPickupData] = useState({
    lat: '',
    lng: '',
    packagePhotoUrl: '',
    notes: ''
  });

  const [deliverData, setDeliverData] = useState({
    recipientName: '',
    recipientRelation: 'Self',
    otp: '',
    podPhotoUrl: '',
    signatureUrl: '',
    deliveredLat: '',
    deliveredLng: '',
    deliveryCondition: 'Good',
    notes: ''
  });

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      const [deliveryResult, stateResult] = await Promise.all([
        deliveryService.getDelivery(id),
        deliveryService.getDeliveryState(id)
      ]);

      setDelivery(deliveryResult);
      setStateInfo(stateResult);

      // Try to get POD if available
      try {
        const podResult = await deliveryService.getPOD(id);
        setPod(podResult);
        setOtpVerified(podResult?.otpVerified || false);
      } catch {
        // POD might not exist yet
      }
    } catch (err) {
      setError(err.message || 'Failed to fetch delivery');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // State flow visualization
  const stateFlow = [
    { status: 'CREATED', label: 'Created', icon: '📝' },
    { status: 'MATCHING', label: 'Matching', icon: '🔍' },
    { status: 'ACCEPTED', label: 'Accepted', icon: '✅' },
    { status: 'PICKED_UP', label: 'Picked Up', icon: '📦' },
    { status: 'IN_TRANSIT', label: 'In Transit', icon: '🚚' },
    { status: 'DELIVERED', label: 'Delivered', icon: '🏠' },
    { status: 'CLOSED', label: 'Closed', icon: '✨' }
  ];

  const getCurrentStepIndex = () => {
    const status = delivery?.status;
    const idx = stateFlow.findIndex(s => s.status === status);
    return idx >= 0 ? idx : 0;
  };

  const getStatusColor = (status) => {
    const colors = {
      'CREATED': '#1976d2',
      'MATCHING': '#f57c00',
      'ASSIGNED': '#388e3c',
      'ACCEPTED': '#388e3c',
      'PICKED_UP': '#0288d1',
      'IN_TRANSIT': '#7b1fa2',
      'DELIVERED': '#2e7d32',
      'CLOSED': '#616161',
      'CANCELLED': '#c62828',
      'UNASSIGNABLE': '#c62828'
    };
    return colors[status] || '#666';
  };

  // Actions
  const handleMatch = async () => {
    try {
      setActionLoading(true);
      await deliveryService.matchDelivery(id);
      setSuccess('Matching started! Looking for delivery partners...');
      await fetchData();
    } catch (err) {
      setError(err.message || 'Failed to start matching');
    } finally {
      setActionLoading(false);
    }
  };

  const handleAccept = async () => {
    try {
      setActionLoading(true);
      await deliveryService.acceptDelivery(id);
      setSuccess('Delivery accepted! Please proceed to pickup location.');
      await fetchData();
    } catch (err) {
      setError(err.message || 'Failed to accept delivery');
    } finally {
      setActionLoading(false);
    }
  };

  const handlePickup = async () => {
    try {
      setActionLoading(true);
      await deliveryService.markAsPickedUp(id, {
        lat: parseFloat(pickupData.lat) || delivery?.pickupLat,
        lng: parseFloat(pickupData.lng) || delivery?.pickupLng,
        packagePhotoUrl: pickupData.packagePhotoUrl || 'https://storage.example.com/package.jpg',
        notes: pickupData.notes
      });
      setSuccess('Package picked up! Now proceed to drop location.');
      setShowPickupModal(false);
      await fetchData();
    } catch (err) {
      setError(err.message || 'Failed to mark as picked up');
    } finally {
      setActionLoading(false);
    }
  };

  const handleTransit = async () => {
    try {
      setActionLoading(true);
      await deliveryService.markAsInTransit(id, {
        lat: delivery?.pickupLat,
        lng: delivery?.pickupLng,
        notes: 'En route to delivery location'
      });
      setSuccess('In transit! Heading to drop location.');
      await fetchData();
    } catch (err) {
      setError(err.message || 'Failed to mark as in transit');
    } finally {
      setActionLoading(false);
    }
  };

  const handleSendOTP = async () => {
    try {
      setActionLoading(true);
      const result = await deliveryService.sendDeliveryOTP(id);
      setOtpSent(true);
      // Extract OTP from dev message if available
      if (result.message?.includes('Dev: OTP is')) {
        const extractedOtp = result.message.split('Dev: OTP is ')[1]?.replace(')', '');
        setOtp(extractedOtp);
        setDeliverData(prev => ({ ...prev, otp: extractedOtp }));
      }
      setSuccess('OTP sent to recipient!');
    } catch (err) {
      setError(err.message || 'Failed to send OTP');
    } finally {
      setActionLoading(false);
    }
  };

  const handleVerifyOTP = async () => {
    try {
      setActionLoading(true);
      const result = await deliveryService.verifyDeliveryOTP(id, otp);
      if (result.isVerified) {
        setOtpVerified(true);
        setSuccess('OTP verified successfully!');
      } else {
        setError('Invalid OTP. Please try again.');
      }
    } catch (err) {
      setError(err.message || 'Failed to verify OTP');
    } finally {
      setActionLoading(false);
    }
  };

  const handleDeliver = async () => {
    try {
      setActionLoading(true);
      await deliveryService.markAsDelivered(id, {
        recipientName: deliverData.recipientName || 'Recipient',
        recipientRelation: deliverData.recipientRelation,
        otp: otp || deliverData.otp,
        podPhotoUrl: deliverData.podPhotoUrl || 'https://storage.example.com/pod.jpg',
        signatureUrl: deliverData.signatureUrl || '',
        deliveredLat: parseFloat(deliverData.deliveredLat) || delivery?.dropLat,
        deliveredLng: parseFloat(deliverData.deliveredLng) || delivery?.dropLng,
        deliveryCondition: deliverData.deliveryCondition,
        notes: deliverData.notes
      });
      setSuccess('Delivery completed successfully!');
      setShowDeliverModal(false);
      await fetchData();
    } catch (err) {
      setError(err.message || 'Failed to complete delivery');
    } finally {
      setActionLoading(false);
    }
  };

  const handleClose = async () => {
    try {
      setActionLoading(true);
      await deliveryService.closeDelivery(id, 'Delivery completed without issues');
      setSuccess('Delivery closed successfully!');
      await fetchData();
    } catch (err) {
      setError(err.message || 'Failed to close delivery');
    } finally {
      setActionLoading(false);
    }
  };

  const handleCancel = async () => {
    const reason = window.prompt('Please enter a reason for cancellation:');
    if (!reason) return; // User cancelled the prompt

    try {
      setActionLoading(true);
      await deliveryService.cancelDelivery(id, reason);
      setSuccess('Delivery cancelled successfully!');
      await fetchData();
    } catch (err) {
      setError(err.message || 'Failed to cancel delivery');
    } finally {
      setActionLoading(false);
    }
  };

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
        <LoadingSpinner message="Loading delivery..." />
      </div>
    );
  }

  if (!delivery) {
    return (
      <div className="container">
        <div className="card" style={{ textAlign: 'center', padding: '50px' }}>
          <h2>Delivery Not Found</h2>
          <button className="btn btn-primary" onClick={() => navigate('/deliveries')}>
            Back to Deliveries
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ maxWidth: '900px', margin: '30px auto' }}>
        {/* Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <div>
            <button
              className="btn btn-outline"
              style={{ marginBottom: '10px' }}
              onClick={() => navigate('/deliveries')}
            >
              Back to Deliveries
            </button>
            <h1 style={{ color: '#2c3e50', margin: 0, fontSize: '24px' }}>
              Delivery #{id?.substring(0, 8)}...
            </h1>
          </div>
          <button className="btn btn-secondary" onClick={fetchData}>
            Refresh
          </button>
        </div>

        {error && <ErrorMessage message={error} onClose={() => setError('')} />}
        {success && (
          <div className="alert alert-success" style={{ marginBottom: '20px' }}>
            {success}
            <button
              onClick={() => setSuccess('')}
              style={{ float: 'right', background: 'none', border: 'none', cursor: 'pointer' }}
            >
              x
            </button>
          </div>
        )}

        {/* State Machine Visualization */}
        <div className="card" style={{ marginBottom: '20px' }}>
          <h2 style={{ fontSize: '18px', color: '#2c3e50', marginBottom: '20px' }}>
            Delivery Progress
          </h2>

          <div style={{ display: 'flex', justifyContent: 'space-between', position: 'relative', marginBottom: '20px' }}>
            {/* Progress Line */}
            <div style={{
              position: 'absolute',
              top: '20px',
              left: '30px',
              right: '30px',
              height: '4px',
              background: '#e0e0e0',
              zIndex: 0
            }}>
              <div style={{
                height: '100%',
                width: `${(getCurrentStepIndex() / (stateFlow.length - 1)) * 100}%`,
                background: getStatusColor(delivery.status),
                transition: 'width 0.5s ease'
              }} />
            </div>

            {stateFlow.map((step, index) => {
              const isActive = delivery.status === step.status;
              const isPast = getCurrentStepIndex() > index;
              const isCurrent = getCurrentStepIndex() === index;

              return (
                <div key={step.status} style={{ textAlign: 'center', zIndex: 1, flex: 1 }}>
                  <div style={{
                    width: '40px',
                    height: '40px',
                    borderRadius: '50%',
                    background: isPast || isCurrent ? getStatusColor(delivery.status) : '#e0e0e0',
                    color: isPast || isCurrent ? '#fff' : '#999',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    margin: '0 auto 8px',
                    fontSize: '18px',
                    border: isCurrent ? '3px solid #333' : 'none',
                    boxShadow: isCurrent ? '0 0 10px rgba(0,0,0,0.2)' : 'none'
                  }}>
                    {step.icon}
                  </div>
                  <p style={{
                    fontSize: '11px',
                    color: isPast || isCurrent ? '#333' : '#999',
                    fontWeight: isCurrent ? '600' : '400'
                  }}>
                    {step.label}
                  </p>
                </div>
              );
            })}
          </div>

          {/* Current Status */}
          <div style={{
            textAlign: 'center',
            padding: '15px',
            background: `${getStatusColor(delivery.status)}15`,
            borderRadius: '8px'
          }}>
            <p style={{ fontSize: '14px', color: '#666', marginBottom: '4px' }}>Current Status</p>
            <p style={{
              fontSize: '24px',
              fontWeight: '700',
              color: getStatusColor(delivery.status)
            }}>
              {delivery.status?.replace('_', ' ')}
            </p>
          </div>
        </div>

        {/* Action Buttons based on state */}
        {stateInfo && (
          <div className="card" style={{ marginBottom: '20px' }}>
            <h2 style={{ fontSize: '18px', color: '#2c3e50', marginBottom: '15px' }}>
              Available Actions
            </h2>

            <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
              {stateInfo.canMatch && (
                <button
                  className="btn btn-primary"
                  onClick={handleMatch}
                  disabled={actionLoading}
                >
                  Start Matching
                </button>
              )}

              {stateInfo.canAccept && (
                <button
                  className="btn btn-primary"
                  onClick={handleAccept}
                  disabled={actionLoading}
                >
                  Accept Delivery
                </button>
              )}

              {stateInfo.canPickup && (
                <button
                  className="btn btn-primary"
                  onClick={() => setShowPickupModal(true)}
                  disabled={actionLoading}
                >
                  Mark as Picked Up
                </button>
              )}

              {stateInfo.canTransit && (
                <button
                  className="btn btn-primary"
                  onClick={handleTransit}
                  disabled={actionLoading}
                >
                  Start Transit
                </button>
              )}

              {stateInfo.canDeliver && (
                <button
                  className="btn btn-primary"
                  onClick={() => setShowDeliverModal(true)}
                  disabled={actionLoading}
                >
                  Complete Delivery
                </button>
              )}

              {stateInfo.canClose && (
                <button
                  className="btn btn-secondary"
                  onClick={handleClose}
                  disabled={actionLoading}
                >
                  Close Delivery
                </button>
              )}

              {stateInfo.canCancel && delivery.status !== 'DELIVERED' && delivery.status !== 'CLOSED' && (
                <button
                  className="btn btn-outline"
                  style={{ color: '#c62828', borderColor: '#c62828' }}
                  onClick={handleCancel}
                  disabled={actionLoading}
                >
                  Cancel Delivery
                </button>
              )}
            </div>

            {stateInfo.allowedTransitions?.length > 0 && (
              <p style={{ fontSize: '12px', color: '#666', marginTop: '15px' }}>
                Allowed transitions: {stateInfo.allowedTransitions.join(', ')}
              </p>
            )}
          </div>
        )}

        {/* Delivery Details */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px', marginBottom: '20px' }}>
          {/* Pickup Info */}
          <div className="card">
            <h3 style={{ fontSize: '14px', color: '#667eea', marginBottom: '15px' }}>PICKUP LOCATION</h3>
            <p style={{ fontWeight: '600', marginBottom: '4px' }}>{delivery.pickupContactName}</p>
            <p style={{ color: '#666', fontSize: '14px', marginBottom: '4px' }}>{delivery.pickupAddress}</p>
            <p style={{ color: '#666', fontSize: '14px', marginBottom: '8px' }}>{delivery.pickupContactPhone}</p>
            {delivery.pickupInstructions && (
              <p style={{ color: '#999', fontSize: '12px', fontStyle: 'italic' }}>
                Note: {delivery.pickupInstructions}
              </p>
            )}
            <a
              href={`https://maps.google.com/?q=${delivery.pickupLat},${delivery.pickupLng}`}
              target="_blank"
              rel="noopener noreferrer"
              className="btn btn-outline"
              style={{ marginTop: '10px', display: 'inline-block' }}
            >
              Open in Maps
            </a>
          </div>

          {/* Drop Info */}
          <div className="card">
            <h3 style={{ fontSize: '14px', color: '#28a745', marginBottom: '15px' }}>DROP LOCATION</h3>
            <p style={{ fontWeight: '600', marginBottom: '4px' }}>{delivery.dropContactName}</p>
            <p style={{ color: '#666', fontSize: '14px', marginBottom: '4px' }}>{delivery.dropAddress}</p>
            <p style={{ color: '#666', fontSize: '14px', marginBottom: '8px' }}>{delivery.dropContactPhone}</p>
            {delivery.dropInstructions && (
              <p style={{ color: '#999', fontSize: '12px', fontStyle: 'italic' }}>
                Note: {delivery.dropInstructions}
              </p>
            )}
            <a
              href={`https://maps.google.com/?q=${delivery.dropLat},${delivery.dropLng}`}
              target="_blank"
              rel="noopener noreferrer"
              className="btn btn-outline"
              style={{ marginTop: '10px', display: 'inline-block' }}
            >
              Open in Maps
            </a>
          </div>
        </div>

        {/* Price & Distance */}
        <div className="card" style={{ marginBottom: '20px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '20px', textAlign: 'center' }}>
            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Distance</p>
              <p style={{ fontSize: '20px', fontWeight: '600' }}>{delivery.distanceKm?.toFixed(2) || '~'} km</p>
            </div>
            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Estimated Price</p>
              <p style={{ fontSize: '20px', fontWeight: '600', color: '#667eea' }}>
                Rs. {delivery.estimatedPrice?.toFixed(2) || '0.00'}
              </p>
            </div>
            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Package Type</p>
              <p style={{ fontSize: '20px', fontWeight: '600' }}>{delivery.packageType || 'Parcel'}</p>
            </div>
            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Weight</p>
              <p style={{ fontSize: '20px', fontWeight: '600' }}>{delivery.packageWeightKg || '~'} kg</p>
            </div>
          </div>
        </div>

        {/* POD Information (if delivered) */}
        {pod && (
          <div className="card" style={{ marginBottom: '20px' }}>
            <h2 style={{ fontSize: '18px', color: '#2c3e50', marginBottom: '15px' }}>
              Proof of Delivery
            </h2>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
              <div>
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Recipient Name</p>
                <p style={{ fontWeight: '500' }}>{pod.recipientName || 'N/A'}</p>
              </div>
              <div>
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Relation</p>
                <p style={{ fontWeight: '500' }}>{pod.recipientRelation || 'N/A'}</p>
              </div>
              <div>
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>OTP Verified</p>
                <p style={{ fontWeight: '500', color: pod.otpVerified ? '#28a745' : '#c62828' }}>
                  {pod.otpVerified ? 'Yes' : 'No'}
                </p>
              </div>
              <div>
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Condition</p>
                <p style={{ fontWeight: '500' }}>{pod.deliveryCondition || 'N/A'}</p>
              </div>
              <div>
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Picked Up At</p>
                <p style={{ fontWeight: '500' }}>{formatDate(pod.pickedUpAt)}</p>
              </div>
              <div>
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Delivered At</p>
                <p style={{ fontWeight: '500' }}>{formatDate(pod.deliveredAt)}</p>
              </div>
            </div>
          </div>
        )}

        {/* Timeline */}
        {delivery.timeline && delivery.timeline.length > 0 && (
          <div className="card">
            <h2 style={{ fontSize: '18px', color: '#2c3e50', marginBottom: '15px' }}>
              Timeline
            </h2>
            <div style={{ borderLeft: '2px solid #e0e0e0', paddingLeft: '20px', marginLeft: '10px' }}>
              {delivery.timeline.map((event, index) => (
                <div key={index} style={{ marginBottom: '15px', position: 'relative' }}>
                  <div style={{
                    position: 'absolute',
                    left: '-26px',
                    width: '12px',
                    height: '12px',
                    borderRadius: '50%',
                    background: getStatusColor(event.toStatus || event.eventType),
                    border: '2px solid #fff'
                  }} />
                  <p style={{ fontSize: '14px', fontWeight: '500', color: '#333' }}>
                    {event.eventType?.replace('_', ' ')}
                  </p>
                  <p style={{ fontSize: '12px', color: '#666' }}>
                    {formatDate(event.timestamp)}
                  </p>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Pickup Modal */}
        {showPickupModal && (
          <div style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            background: 'rgba(0,0,0,0.5)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000
          }}>
            <div className="card" style={{ maxWidth: '500px', width: '90%' }}>
              <h2 style={{ fontSize: '18px', marginBottom: '20px' }}>Confirm Pickup</h2>

              <div className="form-group">
                <label className="form-label">Package Photo URL (optional)</label>
                <input
                  type="text"
                  value={pickupData.packagePhotoUrl}
                  onChange={e => setPickupData(prev => ({ ...prev, packagePhotoUrl: e.target.value }))}
                  className="form-input"
                  placeholder="https://..."
                />
              </div>

              <div className="form-group">
                <label className="form-label">Notes (optional)</label>
                <textarea
                  value={pickupData.notes}
                  onChange={e => setPickupData(prev => ({ ...prev, notes: e.target.value }))}
                  className="form-input"
                  placeholder="Any notes about the pickup..."
                  rows="2"
                />
              </div>

              <div style={{ display: 'flex', gap: '10px', marginTop: '20px' }}>
                <button
                  className="btn btn-secondary"
                  onClick={() => setShowPickupModal(false)}
                  style={{ flex: 1 }}
                >
                  Cancel
                </button>
                <button
                  className="btn btn-primary"
                  onClick={handlePickup}
                  disabled={actionLoading}
                  style={{ flex: 2 }}
                >
                  {actionLoading ? 'Processing...' : 'Confirm Pickup'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Deliver Modal */}
        {showDeliverModal && (
          <div style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            background: 'rgba(0,0,0,0.5)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
            overflow: 'auto'
          }}>
            <div className="card" style={{ maxWidth: '500px', width: '90%', maxHeight: '90vh', overflow: 'auto' }}>
              <h2 style={{ fontSize: '18px', marginBottom: '20px' }}>Complete Delivery</h2>

              {/* OTP Section */}
              <div style={{
                padding: '15px',
                background: '#f5f5f5',
                borderRadius: '8px',
                marginBottom: '20px'
              }}>
                <h3 style={{ fontSize: '14px', marginBottom: '10px' }}>OTP Verification</h3>

                {!otpSent ? (
                  <button
                    className="btn btn-secondary"
                    onClick={handleSendOTP}
                    disabled={actionLoading}
                    style={{ width: '100%' }}
                  >
                    Send OTP to Recipient
                  </button>
                ) : !otpVerified ? (
                  <div>
                    <div className="form-group" style={{ marginBottom: '10px' }}>
                      <input
                        type="text"
                        value={otp}
                        onChange={e => setOtp(e.target.value)}
                        className="form-input"
                        placeholder="Enter 6-digit OTP"
                        maxLength="6"
                      />
                    </div>
                    <button
                      className="btn btn-secondary"
                      onClick={handleVerifyOTP}
                      disabled={actionLoading || otp.length < 4}
                      style={{ width: '100%' }}
                    >
                      Verify OTP
                    </button>
                  </div>
                ) : (
                  <div style={{ color: '#28a745', fontWeight: '500' }}>
                    OTP Verified Successfully!
                  </div>
                )}
              </div>

              <div className="form-group">
                <label className="form-label">Recipient Name *</label>
                <input
                  type="text"
                  value={deliverData.recipientName}
                  onChange={e => setDeliverData(prev => ({ ...prev, recipientName: e.target.value }))}
                  className="form-input"
                  placeholder="Who received the package?"
                  required
                />
              </div>

              <div className="form-group">
                <label className="form-label">Relation to Recipient</label>
                <select
                  value={deliverData.recipientRelation}
                  onChange={e => setDeliverData(prev => ({ ...prev, recipientRelation: e.target.value }))}
                  className="form-input"
                >
                  <option value="Self">Self</option>
                  <option value="Family">Family Member</option>
                  <option value="Neighbor">Neighbor</option>
                  <option value="Security">Security Guard</option>
                  <option value="Other">Other</option>
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Delivery Condition</label>
                <select
                  value={deliverData.deliveryCondition}
                  onChange={e => setDeliverData(prev => ({ ...prev, deliveryCondition: e.target.value }))}
                  className="form-input"
                >
                  <option value="Good">Good</option>
                  <option value="Slightly Damaged">Slightly Damaged</option>
                  <option value="Damaged">Damaged</option>
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">POD Photo URL (optional)</label>
                <input
                  type="text"
                  value={deliverData.podPhotoUrl}
                  onChange={e => setDeliverData(prev => ({ ...prev, podPhotoUrl: e.target.value }))}
                  className="form-input"
                  placeholder="https://..."
                />
              </div>

              <div className="form-group">
                <label className="form-label">Notes (optional)</label>
                <textarea
                  value={deliverData.notes}
                  onChange={e => setDeliverData(prev => ({ ...prev, notes: e.target.value }))}
                  className="form-input"
                  placeholder="Any notes about the delivery..."
                  rows="2"
                />
              </div>

              <div style={{ display: 'flex', gap: '10px', marginTop: '20px' }}>
                <button
                  className="btn btn-secondary"
                  onClick={() => setShowDeliverModal(false)}
                  style={{ flex: 1 }}
                >
                  Cancel
                </button>
                <button
                  className="btn btn-primary"
                  onClick={handleDeliver}
                  disabled={actionLoading || !deliverData.recipientName}
                  style={{ flex: 2 }}
                >
                  {actionLoading ? 'Processing...' : 'Complete Delivery'}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default DeliveryTrackingPage;
