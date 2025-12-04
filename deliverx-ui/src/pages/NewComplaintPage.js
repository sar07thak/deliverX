import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { fileComplaint, getCategories, getSeverityLevels } from '../services/complaintService';
import deliveryService from '../services/deliveryService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

const NewComplaintPage = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [deliveries, setDeliveries] = useState([]);
  const [loadingDeliveries, setLoadingDeliveries] = useState(true);

  const categories = getCategories();
  const severities = getSeverityLevels();

  const [formData, setFormData] = useState({
    deliveryId: '',
    category: '',
    severity: 'MEDIUM',
    subject: '',
    description: ''
  });

  useEffect(() => {
    loadDeliveries();
  }, []);

  const loadDeliveries = async () => {
    try {
      const data = await deliveryService.getMyDeliveries();
      // Backend returns { deliveries: [], totalCount, page, pageSize }
      setDeliveries(data.deliveries || data.Deliveries || []);
    } catch (err) {
      console.error('Failed to load deliveries:', err);
    } finally {
      setLoadingDeliveries(false);
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!formData.deliveryId) {
      setError('Please select a delivery related to this complaint');
      return;
    }

    if (!formData.category || !formData.subject || !formData.description) {
      setError('Please fill in all required fields');
      return;
    }

    try {
      setLoading(true);
      setError('');
      await fileComplaint(formData);
      navigate('/complaints');
    } catch (err) {
      setError(err.message || 'Failed to file complaint');
    } finally {
      setLoading(false);
    }
  };

  if (loadingDeliveries) return <LoadingSpinner />;

  return (
    <div style={{ padding: '20px', maxWidth: '800px', margin: '0 auto' }}>
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
        <h1 style={{ margin: 0 }}>File New Complaint</h1>
      </div>

      {error && <ErrorMessage message={error} onClose={() => setError('')} />}

      <form onSubmit={handleSubmit} style={{ background: 'white', padding: '30px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
        {/* Related Delivery (Required) */}
        <div style={{ marginBottom: '20px' }}>
          <label style={{ display: 'block', marginBottom: '5px', fontWeight: '500' }}>
            Related Delivery <span style={{ color: 'red' }}>*</span>
          </label>
          <select
            name="deliveryId"
            value={formData.deliveryId}
            onChange={handleChange}
            style={{
              width: '100%',
              padding: '12px',
              borderRadius: '5px',
              border: '1px solid #ddd',
              fontSize: '1em'
            }}
          >
            <option value="">-- Select a delivery --</option>
            {deliveries.map(d => (
              <option key={d.id} value={d.id}>
                #{d.id?.substring(0, 8)}... - {d.status} - {d.pickupAddress?.substring(0, 20)}...
              </option>
            ))}
          </select>
        </div>

        {/* Category */}
        <div style={{ marginBottom: '20px' }}>
          <label style={{ display: 'block', marginBottom: '5px', fontWeight: '500' }}>
            Category <span style={{ color: 'red' }}>*</span>
          </label>
          <select
            name="category"
            value={formData.category}
            onChange={handleChange}
            required
            style={{
              width: '100%',
              padding: '12px',
              borderRadius: '5px',
              border: '1px solid #ddd',
              fontSize: '1em'
            }}
          >
            <option value="">-- Select category --</option>
            {categories.map(cat => (
              <option key={cat.value} value={cat.value}>{cat.label}</option>
            ))}
          </select>
        </div>

        {/* Severity */}
        <div style={{ marginBottom: '20px' }}>
          <label style={{ display: 'block', marginBottom: '5px', fontWeight: '500' }}>
            Severity <span style={{ color: 'red' }}>*</span>
          </label>
          <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
            {severities.map(sev => (
              <label
                key={sev.value}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  padding: '10px 15px',
                  borderRadius: '5px',
                  border: formData.severity === sev.value ? `2px solid ${sev.color}` : '2px solid #ddd',
                  cursor: 'pointer',
                  background: formData.severity === sev.value ? `${sev.color}20` : 'white'
                }}
              >
                <input
                  type="radio"
                  name="severity"
                  value={sev.value}
                  checked={formData.severity === sev.value}
                  onChange={handleChange}
                  style={{ marginRight: '8px' }}
                />
                <span style={{
                  padding: '2px 8px',
                  borderRadius: '4px',
                  background: sev.color,
                  color: 'white',
                  fontSize: '0.85em',
                  fontWeight: '600'
                }}>
                  {sev.label}
                </span>
              </label>
            ))}
          </div>
        </div>

        {/* Subject */}
        <div style={{ marginBottom: '20px' }}>
          <label style={{ display: 'block', marginBottom: '5px', fontWeight: '500' }}>
            Subject <span style={{ color: 'red' }}>*</span>
          </label>
          <input
            type="text"
            name="subject"
            value={formData.subject}
            onChange={handleChange}
            placeholder="Brief description of the issue"
            required
            style={{
              width: '100%',
              padding: '12px',
              borderRadius: '5px',
              border: '1px solid #ddd',
              fontSize: '1em',
              boxSizing: 'border-box'
            }}
          />
        </div>

        {/* Description */}
        <div style={{ marginBottom: '20px' }}>
          <label style={{ display: 'block', marginBottom: '5px', fontWeight: '500' }}>
            Description <span style={{ color: 'red' }}>*</span>
          </label>
          <textarea
            name="description"
            value={formData.description}
            onChange={handleChange}
            placeholder="Please provide detailed information about your complaint..."
            required
            rows={6}
            style={{
              width: '100%',
              padding: '12px',
              borderRadius: '5px',
              border: '1px solid #ddd',
              fontSize: '1em',
              resize: 'vertical',
              boxSizing: 'border-box'
            }}
          />
        </div>

        {/* Submit Buttons */}
        <div style={{ display: 'flex', gap: '10px' }}>
          <button
            type="button"
            onClick={() => navigate('/complaints')}
            style={{
              flex: 1,
              padding: '12px',
              border: '1px solid #ddd',
              borderRadius: '8px',
              background: 'white',
              cursor: 'pointer',
              fontSize: '1em'
            }}
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={loading}
            style={{
              flex: 1,
              padding: '12px',
              border: 'none',
              borderRadius: '8px',
              background: '#dc3545',
              color: 'white',
              cursor: loading ? 'not-allowed' : 'pointer',
              fontWeight: '600',
              fontSize: '1em'
            }}
          >
            {loading ? 'Submitting...' : 'Submit Complaint'}
          </button>
        </div>
      </form>
    </div>
  );
};

export default NewComplaintPage;
