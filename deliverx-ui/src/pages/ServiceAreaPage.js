import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import serviceAreaService from '../services/serviceAreaService';
import deliveryService from '../services/deliveryService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

const ServiceAreaPage = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [serviceArea, setServiceArea] = useState(null);
  const [availability, setAvailability] = useState(null);

  const [formData, setFormData] = useState({
    centerLat: '',
    centerLng: '',
    radiusKm: '10',
    areaName: ''
  });

  const [availabilityStatus, setAvailabilityStatus] = useState('OFFLINE');

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      // Fetch service area
      try {
        const areaResult = await serviceAreaService.getMyServiceArea();
        // API returns { serviceAreas: [...] } - get the first active one
        const activeArea = areaResult?.serviceAreas?.[0] || null;
        setServiceArea(activeArea);
        if (activeArea) {
          setFormData({
            centerLat: activeArea.centerLat?.toString() || '',
            centerLng: activeArea.centerLng?.toString() || '',
            radiusKm: activeArea.radiusKm?.toString() || '10',
            areaName: activeArea.areaName || ''
          });
        }
      } catch {
        // No service area set yet
      }

      // Fetch availability
      try {
        const availResult = await deliveryService.getAvailability();
        setAvailability(availResult);
        setAvailabilityStatus(availResult?.status || 'OFFLINE');
      } catch {
        // No availability set yet
      }
    } catch (err) {
      setError(err.message || 'Failed to fetch data');
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSaveServiceArea = async (e) => {
    e.preventDefault();
    try {
      setSaving(true);
      setError('');

      await serviceAreaService.setServiceArea({
        centerLat: parseFloat(formData.centerLat),
        centerLng: parseFloat(formData.centerLng),
        radiusKm: parseFloat(formData.radiusKm),
        areaName: formData.areaName
      });

      setSuccess('Service area saved successfully!');
      await fetchData();
    } catch (err) {
      setError(err.message || 'Failed to save service area');
    } finally {
      setSaving(false);
    }
  };

  const handleUpdateAvailability = async (status) => {
    try {
      setSaving(true);
      setError('');

      await deliveryService.updateAvailability({
        status: status,
        lat: parseFloat(formData.centerLat) || null,
        lng: parseFloat(formData.centerLng) || null
      });

      setAvailabilityStatus(status);
      setSuccess(`Availability updated to ${status}`);
      await fetchData();
    } catch (err) {
      setError(err.message || 'Failed to update availability');
    } finally {
      setSaving(false);
    }
  };

  // Pre-fill with Jaipur test coordinates
  const useJaipurCoordinates = () => {
    setFormData({
      centerLat: '26.9124',
      centerLng: '75.7873',
      radiusKm: '15',
      areaName: 'Jaipur Central'
    });
  };

  const getAvailabilityColor = (status) => {
    const colors = {
      'AVAILABLE': { bg: '#c8e6c9', color: '#2e7d32' },
      'BUSY': { bg: '#fff3e0', color: '#f57c00' },
      'OFFLINE': { bg: '#f5f5f5', color: '#616161' },
      'ON_BREAK': { bg: '#e3f2fd', color: '#1976d2' }
    };
    return colors[status] || colors['OFFLINE'];
  };

  if (loading) {
    return (
      <div className="container">
        <LoadingSpinner message="Loading service area..." />
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ maxWidth: '800px', margin: '30px auto' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <h1 style={{ color: '#2c3e50', margin: 0 }}>Service Area & Availability</h1>
          <button
            className="btn btn-outline"
            onClick={() => navigate('/dashboard')}
          >
            Back to Dashboard
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

        {/* Current Status */}
        <div className="card" style={{ marginBottom: '20px' }}>
          <h2 style={{ fontSize: '18px', color: '#2c3e50', marginBottom: '15px' }}>
            Current Status
          </h2>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '8px' }}>Service Area</p>
              {serviceArea ? (
                <div style={{
                  padding: '15px',
                  background: '#e8f5e9',
                  borderRadius: '8px'
                }}>
                  <p style={{ fontWeight: '600', color: '#2e7d32', marginBottom: '4px' }}>
                    {serviceArea.areaName || 'Custom Area'}
                  </p>
                  <p style={{ fontSize: '13px', color: '#666' }}>
                    Radius: {serviceArea.radiusKm} km
                  </p>
                  <p style={{ fontSize: '12px', color: '#999' }}>
                    Center: {serviceArea.centerLat?.toFixed(4)}, {serviceArea.centerLng?.toFixed(4)}
                  </p>
                </div>
              ) : (
                <div style={{
                  padding: '15px',
                  background: '#fff3e0',
                  borderRadius: '8px'
                }}>
                  <p style={{ fontWeight: '600', color: '#f57c00' }}>
                    No service area set
                  </p>
                  <p style={{ fontSize: '13px', color: '#666' }}>
                    Please configure your service area below
                  </p>
                </div>
              )}
            </div>

            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '8px' }}>Availability</p>
              <div style={{
                padding: '15px',
                background: getAvailabilityColor(availabilityStatus).bg,
                borderRadius: '8px'
              }}>
                <p style={{
                  fontWeight: '600',
                  color: getAvailabilityColor(availabilityStatus).color,
                  fontSize: '18px',
                  marginBottom: '4px'
                }}>
                  {availabilityStatus}
                </p>
                {availability?.currentDeliveryId && (
                  <p style={{ fontSize: '13px', color: '#666' }}>
                    Currently on delivery
                  </p>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Availability Controls */}
        <div className="card" style={{ marginBottom: '20px' }}>
          <h2 style={{ fontSize: '18px', color: '#2c3e50', marginBottom: '15px' }}>
            Set Availability
          </h2>

          <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
            <button
              className={availabilityStatus === 'AVAILABLE' ? 'btn btn-primary' : 'btn btn-outline'}
              onClick={() => handleUpdateAvailability('AVAILABLE')}
              disabled={saving || !serviceArea}
              style={{ flex: 1, minWidth: '120px' }}
            >
              Available
            </button>
            <button
              className={availabilityStatus === 'ON_BREAK' ? 'btn btn-primary' : 'btn btn-outline'}
              onClick={() => handleUpdateAvailability('ON_BREAK')}
              disabled={saving}
              style={{ flex: 1, minWidth: '120px' }}
            >
              On Break
            </button>
            <button
              className={availabilityStatus === 'OFFLINE' ? 'btn btn-primary' : 'btn btn-outline'}
              onClick={() => handleUpdateAvailability('OFFLINE')}
              disabled={saving}
              style={{ flex: 1, minWidth: '120px' }}
            >
              Offline
            </button>
          </div>

          {!serviceArea && (
            <p style={{ fontSize: '12px', color: '#f57c00', marginTop: '10px' }}>
              Please set your service area before going available
            </p>
          )}
        </div>

        {/* Service Area Form */}
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
            <h2 style={{ fontSize: '18px', color: '#2c3e50', margin: 0 }}>
              {serviceArea ? 'Update Service Area' : 'Set Service Area'}
            </h2>
            <button
              type="button"
              className="btn btn-secondary"
              style={{ fontSize: '12px', padding: '5px 10px' }}
              onClick={useJaipurCoordinates}
            >
              Use Jaipur Coordinates
            </button>
          </div>

          <form onSubmit={handleSaveServiceArea}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
              <div className="form-group">
                <label className="form-label">Center Latitude *</label>
                <input
                  type="text"
                  name="centerLat"
                  value={formData.centerLat}
                  onChange={handleChange}
                  className="form-input"
                  placeholder="e.g., 26.9124"
                  required
                />
              </div>
              <div className="form-group">
                <label className="form-label">Center Longitude *</label>
                <input
                  type="text"
                  name="centerLng"
                  value={formData.centerLng}
                  onChange={handleChange}
                  className="form-input"
                  placeholder="e.g., 75.7873"
                  required
                />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
              <div className="form-group">
                <label className="form-label">Service Radius (km) *</label>
                <input
                  type="number"
                  name="radiusKm"
                  value={formData.radiusKm}
                  onChange={handleChange}
                  className="form-input"
                  placeholder="e.g., 10"
                  min="1"
                  max="50"
                  required
                />
              </div>
              <div className="form-group">
                <label className="form-label">Area Name (optional)</label>
                <input
                  type="text"
                  name="areaName"
                  value={formData.areaName}
                  onChange={handleChange}
                  className="form-input"
                  placeholder="e.g., Downtown, City Center"
                />
              </div>
            </div>

            <button
              type="submit"
              className="btn btn-primary"
              style={{ width: '100%', marginTop: '10px' }}
              disabled={saving}
            >
              {saving ? 'Saving...' : (serviceArea ? 'Update Service Area' : 'Save Service Area')}
            </button>
          </form>

          {/* Map Placeholder */}
          <div style={{
            marginTop: '20px',
            padding: '30px',
            background: '#f5f5f5',
            borderRadius: '8px',
            textAlign: 'center'
          }}>
            <p style={{ color: '#666', marginBottom: '10px' }}>Map Preview</p>
            {formData.centerLat && formData.centerLng ? (
              <a
                href={`https://maps.google.com/?q=${formData.centerLat},${formData.centerLng}`}
                target="_blank"
                rel="noopener noreferrer"
                className="btn btn-outline"
              >
                View on Google Maps
              </a>
            ) : (
              <p style={{ color: '#999', fontSize: '14px' }}>Enter coordinates to view on map</p>
            )}
          </div>
        </div>

        {/* Help Text */}
        <div className="card" style={{ marginTop: '20px', background: '#f0f7ff' }}>
          <h3 style={{ fontSize: '16px', color: '#1976d2', marginBottom: '10px' }}>
            How Service Area Works
          </h3>
          <ul style={{ margin: 0, paddingLeft: '20px', color: '#666', fontSize: '14px' }}>
            <li style={{ marginBottom: '8px' }}>
              Set your service center point (where you usually start from)
            </li>
            <li style={{ marginBottom: '8px' }}>
              Define a radius within which you're willing to pick up deliveries
            </li>
            <li style={{ marginBottom: '8px' }}>
              You'll only receive delivery requests within your service area
            </li>
            <li>
              Set yourself as "Available" to start receiving delivery requests
            </li>
          </ul>
        </div>
      </div>
    </div>
  );
};

export default ServiceAreaPage;
