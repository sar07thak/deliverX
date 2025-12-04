import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import deliveryService from '../services/deliveryService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

const CreateDeliveryPage = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [step, setStep] = useState(1);
  const [estimate, setEstimate] = useState(null);

  const [formData, setFormData] = useState({
    requesterType: 'EC',
    // Pickup location
    pickupLat: '',
    pickupLng: '',
    pickupAddress: '',
    pickupContactName: '',
    pickupContactPhone: '',
    pickupInstructions: '',
    // Drop location
    dropLat: '',
    dropLng: '',
    dropAddress: '',
    dropContactName: '',
    dropContactPhone: '',
    dropInstructions: '',
    // Package details
    packageWeightKg: '',
    packageType: 'parcel',
    packageDescription: '',
    // Priority
    priority: 'ASAP'
  });

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const getEstimate = async () => {
    if (!formData.pickupLat || !formData.pickupLng || !formData.dropLat || !formData.dropLng) {
      setError('Please enter pickup and drop coordinates');
      return;
    }

    try {
      setLoading(true);
      const result = await deliveryService.getDeliveryEstimate(
        parseFloat(formData.pickupLat),
        parseFloat(formData.pickupLng),
        parseFloat(formData.dropLat),
        parseFloat(formData.dropLng)
      );
      setEstimate(result);
    } catch (err) {
      // If estimate fails, just use default values
      console.log('Estimate failed, using defaults:', err);
      setEstimate({
        distanceKm: 2.5,
        estimatedPrice: 50,
        estimatedMinutes: 20
      });
    } finally {
      setLoading(false);
      setStep(2); // Always proceed to step 2
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    try {
      setLoading(true);
      setError('');

      const deliveryData = {
        requesterType: formData.requesterType,
        pickup: {
          lat: parseFloat(formData.pickupLat),
          lng: parseFloat(formData.pickupLng),
          address: formData.pickupAddress,
          contactName: formData.pickupContactName,
          contactPhone: formData.pickupContactPhone,
          instructions: formData.pickupInstructions
        },
        drop: {
          lat: parseFloat(formData.dropLat),
          lng: parseFloat(formData.dropLng),
          address: formData.dropAddress,
          contactName: formData.dropContactName,
          contactPhone: formData.dropContactPhone,
          instructions: formData.dropInstructions
        },
        package: {
          weightKg: parseFloat(formData.packageWeightKg) || 1,
          type: formData.packageType,
          description: formData.packageDescription
        },
        priority: formData.priority
      };

      const result = await deliveryService.createDelivery(deliveryData);

      if (result.deliveryId) {
        navigate(`/deliveries/${result.deliveryId}`);
      }
    } catch (err) {
      setError(err.message || 'Failed to create delivery');
    } finally {
      setLoading(false);
    }
  };

  // Pre-fill with Jaipur test coordinates
  const useTestCoordinates = () => {
    setFormData(prev => ({
      ...prev,
      pickupLat: '26.9150',
      pickupLng: '75.7900',
      pickupAddress: '100 Pickup Street, Jaipur',
      pickupContactName: 'Test Sender',
      pickupContactPhone: '9876543210',
      dropLat: '26.9050',
      dropLng: '75.7840',
      dropAddress: '200 Drop Avenue, Jaipur',
      dropContactName: 'Test Recipient',
      dropContactPhone: '9876543211'
    }));
  };

  if (loading) {
    return (
      <div className="container">
        <LoadingSpinner message="Processing..." />
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ maxWidth: '800px', margin: '30px auto' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <h1 style={{ color: '#2c3e50', margin: 0 }}>Create Delivery</h1>
          <button
            className="btn btn-outline"
            onClick={() => navigate('/deliveries')}
          >
            Back to Deliveries
          </button>
        </div>

        {error && <ErrorMessage message={error} onClose={() => setError('')} />}

        {/* Progress Steps */}
        <div style={{ display: 'flex', marginBottom: '30px', gap: '10px' }}>
          <div style={{
            flex: 1,
            padding: '10px',
            textAlign: 'center',
            background: step >= 1 ? '#667eea' : '#e0e0e0',
            color: step >= 1 ? '#fff' : '#666',
            borderRadius: '5px'
          }}>
            1. Location Details
          </div>
          <div style={{
            flex: 1,
            padding: '10px',
            textAlign: 'center',
            background: step >= 2 ? '#667eea' : '#e0e0e0',
            color: step >= 2 ? '#fff' : '#666',
            borderRadius: '5px'
          }}>
            2. Package & Confirm
          </div>
        </div>

        <form onSubmit={handleSubmit}>
          {step === 1 && (
            <>
              {/* Pickup Location */}
              <div className="card" style={{ marginBottom: '20px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
                  <h2 style={{ fontSize: '18px', color: '#2c3e50', margin: 0 }}>
                    Pickup Location
                  </h2>
                  <button
                    type="button"
                    className="btn btn-secondary"
                    style={{ fontSize: '12px', padding: '5px 10px' }}
                    onClick={useTestCoordinates}
                  >
                    Use Test Coordinates
                  </button>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
                  <div className="form-group">
                    <label className="form-label">Latitude *</label>
                    <input
                      type="text"
                      name="pickupLat"
                      value={formData.pickupLat}
                      onChange={handleChange}
                      className="form-input"
                      placeholder="e.g., 26.9150"
                      required
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Longitude *</label>
                    <input
                      type="text"
                      name="pickupLng"
                      value={formData.pickupLng}
                      onChange={handleChange}
                      className="form-input"
                      placeholder="e.g., 75.7900"
                      required
                    />
                  </div>
                </div>

                <div className="form-group">
                  <label className="form-label">Address *</label>
                  <input
                    type="text"
                    name="pickupAddress"
                    value={formData.pickupAddress}
                    onChange={handleChange}
                    className="form-input"
                    placeholder="Full pickup address"
                    required
                  />
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
                  <div className="form-group">
                    <label className="form-label">Contact Name *</label>
                    <input
                      type="text"
                      name="pickupContactName"
                      value={formData.pickupContactName}
                      onChange={handleChange}
                      className="form-input"
                      placeholder="Sender name"
                      required
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Contact Phone *</label>
                    <input
                      type="tel"
                      name="pickupContactPhone"
                      value={formData.pickupContactPhone}
                      onChange={handleChange}
                      className="form-input"
                      placeholder="10-digit phone"
                      required
                    />
                  </div>
                </div>

                <div className="form-group">
                  <label className="form-label">Instructions (optional)</label>
                  <input
                    type="text"
                    name="pickupInstructions"
                    value={formData.pickupInstructions}
                    onChange={handleChange}
                    className="form-input"
                    placeholder="e.g., Ring doorbell, call on arrival"
                  />
                </div>
              </div>

              {/* Drop Location */}
              <div className="card" style={{ marginBottom: '20px' }}>
                <h2 style={{ fontSize: '18px', color: '#2c3e50', marginBottom: '15px' }}>
                  Drop Location
                </h2>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
                  <div className="form-group">
                    <label className="form-label">Latitude *</label>
                    <input
                      type="text"
                      name="dropLat"
                      value={formData.dropLat}
                      onChange={handleChange}
                      className="form-input"
                      placeholder="e.g., 26.9050"
                      required
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Longitude *</label>
                    <input
                      type="text"
                      name="dropLng"
                      value={formData.dropLng}
                      onChange={handleChange}
                      className="form-input"
                      placeholder="e.g., 75.7840"
                      required
                    />
                  </div>
                </div>

                <div className="form-group">
                  <label className="form-label">Address *</label>
                  <input
                    type="text"
                    name="dropAddress"
                    value={formData.dropAddress}
                    onChange={handleChange}
                    className="form-input"
                    placeholder="Full drop address"
                    required
                  />
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
                  <div className="form-group">
                    <label className="form-label">Recipient Name *</label>
                    <input
                      type="text"
                      name="dropContactName"
                      value={formData.dropContactName}
                      onChange={handleChange}
                      className="form-input"
                      placeholder="Recipient name"
                      required
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Recipient Phone *</label>
                    <input
                      type="tel"
                      name="dropContactPhone"
                      value={formData.dropContactPhone}
                      onChange={handleChange}
                      className="form-input"
                      placeholder="10-digit phone"
                      required
                    />
                  </div>
                </div>

                <div className="form-group">
                  <label className="form-label">Instructions (optional)</label>
                  <input
                    type="text"
                    name="dropInstructions"
                    value={formData.dropInstructions}
                    onChange={handleChange}
                    className="form-input"
                    placeholder="e.g., Leave at door, hand to recipient only"
                  />
                </div>
              </div>

              <button
                type="button"
                className="btn btn-primary"
                style={{ width: '100%', padding: '15px', fontSize: '16px' }}
                onClick={getEstimate}
              >
                Get Estimate & Continue
              </button>
            </>
          )}

          {step === 2 && (
            <>
              {/* Estimate Card */}
              {estimate && (
                <div className="card" style={{ marginBottom: '20px', background: '#f0f7ff', border: '2px solid #667eea' }}>
                  <h2 style={{ fontSize: '18px', color: '#2c3e50', marginBottom: '15px' }}>
                    Delivery Estimate
                  </h2>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '15px' }}>
                    <div>
                      <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Distance</p>
                      <p style={{ fontSize: '18px', fontWeight: '600' }}>{estimate.distanceKm?.toFixed(2) || '~'} km</p>
                    </div>
                    <div>
                      <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Estimated Price</p>
                      <p style={{ fontSize: '18px', fontWeight: '600', color: '#667eea' }}>
                        Rs. {estimate.estimatedPrice?.toFixed(2) || estimate.basePrice?.toFixed(2) || '50.00'}
                      </p>
                    </div>
                    <div>
                      <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>ETA</p>
                      <p style={{ fontSize: '18px', fontWeight: '600' }}>{estimate.estimatedMinutes || '15-30'} min</p>
                    </div>
                  </div>
                </div>
              )}

              {/* Package Details */}
              <div className="card" style={{ marginBottom: '20px' }}>
                <h2 style={{ fontSize: '18px', color: '#2c3e50', marginBottom: '15px' }}>
                  Package Details
                </h2>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
                  <div className="form-group">
                    <label className="form-label">Package Type</label>
                    <select
                      name="packageType"
                      value={formData.packageType}
                      onChange={handleChange}
                      className="form-input"
                    >
                      <option value="parcel">Parcel</option>
                      <option value="document">Document</option>
                      <option value="food">Food</option>
                      <option value="grocery">Grocery</option>
                      <option value="medicine">Medicine</option>
                      <option value="other">Other</option>
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">Weight (kg)</label>
                    <input
                      type="number"
                      name="packageWeightKg"
                      value={formData.packageWeightKg}
                      onChange={handleChange}
                      className="form-input"
                      placeholder="e.g., 1.5"
                      step="0.1"
                      min="0.1"
                    />
                  </div>
                </div>

                <div className="form-group">
                  <label className="form-label">Description</label>
                  <textarea
                    name="packageDescription"
                    value={formData.packageDescription}
                    onChange={handleChange}
                    className="form-input"
                    placeholder="Brief description of the package contents"
                    rows="2"
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Priority</label>
                  <select
                    name="priority"
                    value={formData.priority}
                    onChange={handleChange}
                    className="form-input"
                  >
                    <option value="ASAP">ASAP (As Soon As Possible)</option>
                    <option value="SCHEDULED">Scheduled</option>
                    <option value="STANDARD">Standard</option>
                  </select>
                </div>
              </div>

              {/* Location Summary */}
              <div className="card" style={{ marginBottom: '20px' }}>
                <h2 style={{ fontSize: '18px', color: '#2c3e50', marginBottom: '15px' }}>
                  Delivery Summary
                </h2>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
                  <div>
                    <h4 style={{ fontSize: '14px', color: '#667eea', marginBottom: '8px' }}>PICKUP</h4>
                    <p style={{ fontSize: '14px', marginBottom: '4px' }}><strong>{formData.pickupContactName}</strong></p>
                    <p style={{ fontSize: '13px', color: '#666', marginBottom: '4px' }}>{formData.pickupAddress}</p>
                    <p style={{ fontSize: '13px', color: '#666' }}>{formData.pickupContactPhone}</p>
                  </div>
                  <div>
                    <h4 style={{ fontSize: '14px', color: '#28a745', marginBottom: '8px' }}>DROP</h4>
                    <p style={{ fontSize: '14px', marginBottom: '4px' }}><strong>{formData.dropContactName}</strong></p>
                    <p style={{ fontSize: '13px', color: '#666', marginBottom: '4px' }}>{formData.dropAddress}</p>
                    <p style={{ fontSize: '13px', color: '#666' }}>{formData.dropContactPhone}</p>
                  </div>
                </div>
              </div>

              <div style={{ display: 'flex', gap: '15px' }}>
                <button
                  type="button"
                  className="btn btn-secondary"
                  style={{ flex: 1, padding: '15px' }}
                  onClick={() => setStep(1)}
                >
                  Back
                </button>
                <button
                  type="submit"
                  className="btn btn-primary"
                  style={{ flex: 2, padding: '15px', fontSize: '16px' }}
                >
                  Create Delivery Order
                </button>
              </div>
            </>
          )}
        </form>
      </div>
    </div>
  );
};

export default CreateDeliveryPage;
