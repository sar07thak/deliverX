import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import registrationService from '../services/registrationService';
import StepIndicator from '../components/StepIndicator';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

/**
 * Registration Page Component
 * Multi-step wizard for user profile completion based on role
 */
const RegistrationPage = () => {
  const { user, updateUser } = useAuth();
  const navigate = useNavigate();

  const [currentStep, setCurrentStep] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  // Get user role - default to EC if not set
  const userRole = user?.role || 'EC';

  // Form data state - common fields for all roles
  const [formData, setFormData] = useState({
    // Personal Information (all roles)
    name: '',
    dateOfBirth: '',
    gender: '',
    email: '',

    // Address (all roles)
    address: {
      street: '',
      city: '',
      state: '',
      pincode: ''
    },

    // Business Consumer specific
    businessName: '',
    gstin: '',
    businessType: '',

    // Delivery Partner specific
    vehicleType: '',
    languagesKnown: [],
    availability: '',
    serviceArea: '',
    pricing: {
      baseRate: '',
      perKmRate: '',
      perMinuteRate: ''
    }
  });

  // Steps vary by role
  const getSteps = () => {
    switch (userRole) {
      case 'DP':
        return ['Personal Info', 'Service Details', 'Review & Submit'];
      case 'BC':
      case 'DBC':
        return ['Personal Info', 'Business Details', 'Review & Submit'];
      default: // EC
        return ['Personal Info', 'Review & Submit'];
    }
  };

  const steps = getSteps();
  const totalSteps = steps.length;

  // Handle input changes
  const handleChange = (e) => {
    const { name, value } = e.target;

    if (name.includes('.')) {
      const [parent, child] = name.split('.');
      setFormData(prev => ({
        ...prev,
        [parent]: {
          ...prev[parent],
          [child]: value
        }
      }));
    } else {
      setFormData(prev => ({
        ...prev,
        [name]: value
      }));
    }
  };

  // Handle language selection (DP only)
  const handleLanguageChange = (language) => {
    setFormData(prev => ({
      ...prev,
      languagesKnown: prev.languagesKnown.includes(language)
        ? prev.languagesKnown.filter(lang => lang !== language)
        : [...prev.languagesKnown, language]
    }));
  };

  // Validate current step
  const validateStep = () => {
    setError('');

    // Step 1: Personal Info (all roles)
    if (currentStep === 1) {
      if (!formData.name || !formData.email) {
        setError('Please fill name and email');
        return false;
      }
      if (!formData.address.city || !formData.address.state || !formData.address.pincode) {
        setError('Please fill address fields');
        return false;
      }
      if (formData.address.pincode.length !== 6) {
        setError('Pincode must be 6 digits');
        return false;
      }
    }

    // Step 2 for DP: Service Details
    if (currentStep === 2 && userRole === 'DP') {
      if (!formData.vehicleType || !formData.availability || !formData.serviceArea) {
        setError('Please fill all service details');
        return false;
      }
      if (formData.languagesKnown.length === 0) {
        setError('Please select at least one language');
        return false;
      }
      if (!formData.pricing.baseRate || !formData.pricing.perKmRate) {
        setError('Please fill pricing fields');
        return false;
      }
    }

    // Step 2 for BC: Business Details
    if (currentStep === 2 && (userRole === 'BC' || userRole === 'DBC')) {
      if (!formData.businessName || !formData.businessType) {
        setError('Please fill business details');
        return false;
      }
    }

    return true;
  };

  // Navigate to next step
  const handleNext = () => {
    if (validateStep()) {
      setCurrentStep(prev => prev + 1);
    }
  };

  // Navigate to previous step
  const handlePrevious = () => {
    setError('');
    setCurrentStep(prev => prev - 1);
  };

  // Submit registration
  const handleSubmit = async () => {
    if (!validateStep()) return;

    setLoading(true);
    setError('');

    try {
      let profileData = {
        fullName: formData.name,
        dob: formData.dateOfBirth || null,
        gender: formData.gender || null,
        email: formData.email,
        address: {
          line1: formData.address.street,
          line2: '',
          city: formData.address.city,
          state: formData.address.state,
          pincode: formData.address.pincode
        }
      };

      // Add role-specific data
      if (userRole === 'DP') {
        profileData = {
          ...profileData,
          vehicleType: formData.vehicleType,
          languages: formData.languagesKnown,
          availability: formData.availability,
          serviceArea: {
            centerLat: 12.9716,
            centerLng: 77.5946,
            radiusKm: 10
          },
          pricing: {
            perKmRate: parseFloat(formData.pricing.perKmRate),
            perKgRate: parseFloat(formData.pricing.baseRate),
            minCharge: 20.00,
            maxDistanceKm: 50
          }
        };
      } else if (userRole === 'BC' || userRole === 'DBC') {
        profileData = {
          ...profileData,
          businessName: formData.businessName,
          gstin: formData.gstin,
          businessType: formData.businessType
        };
      }

      console.log('Submitting profile data:', profileData);
      const response = await registrationService.completeProfile(profileData);

      console.log('Profile completion response:', response);

      if (response.success) {
        const updatedUser = {
          ...user,
          profileComplete: true,
          name: profileData.fullName,
          email: profileData.email
        };

        localStorage.setItem('user', JSON.stringify(updatedUser));
        updateUser(updatedUser);

        // Redirect DP to KYC, others to dashboard
        if (userRole === 'DP') {
          navigate('/kyc');
        } else {
          navigate('/dashboard');
        }
      } else {
        setError(response.message || 'Registration failed');
      }
    } catch (err) {
      console.error('Registration error:', err);
      const errorMessage = err.response?.data?.message || err.message || 'Failed to complete registration.';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  // Render role-specific title
  const getRoleTitle = () => {
    switch (userRole) {
      case 'DP':
        return 'Delivery Partner Registration';
      case 'BC':
      case 'DBC':
        return 'Business Account Registration';
      default:
        return 'Complete Your Profile';
    }
  };

  return (
    <div className="container">
      <div style={{ maxWidth: '800px', margin: '30px auto' }}>
        <div className="card">
          <h1 style={{ marginBottom: '10px', color: '#2c3e50' }}>
            {getRoleTitle()}
          </h1>
          <p style={{ marginBottom: '30px', color: '#666' }}>
            Account Type: <strong>{userRole === 'EC' ? 'End Consumer' : userRole === 'BC' || userRole === 'DBC' ? 'Business Consumer' : 'Delivery Partner'}</strong>
          </p>

          <StepIndicator
            currentStep={currentStep}
            totalSteps={totalSteps}
            steps={steps}
          />

          {error && <ErrorMessage message={error} onClose={() => setError('')} />}

          {/* Step 1: Personal Information (All Roles) */}
          {currentStep === 1 && (
            <div>
              <h2 style={{ marginBottom: '20px', fontSize: '18px', color: '#2c3e50' }}>
                Personal Information
              </h2>

              <div className="form-group">
                <label className="form-label">Full Name *</label>
                <input
                  type="text"
                  name="name"
                  className="form-input"
                  placeholder="Enter your full name"
                  value={formData.name}
                  onChange={handleChange}
                  required
                />
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group">
                  <label className="form-label">Date of Birth</label>
                  <input
                    type="date"
                    name="dateOfBirth"
                    className="form-input"
                    value={formData.dateOfBirth}
                    onChange={handleChange}
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Gender</label>
                  <select
                    name="gender"
                    className="form-select"
                    value={formData.gender}
                    onChange={handleChange}
                  >
                    <option value="">Select Gender</option>
                    <option value="Male">Male</option>
                    <option value="Female">Female</option>
                    <option value="Other">Other</option>
                  </select>
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Email Address *</label>
                <input
                  type="email"
                  name="email"
                  className="form-input"
                  placeholder="your.email@example.com"
                  value={formData.email}
                  onChange={handleChange}
                  required
                />
              </div>

              <h3 style={{ marginTop: '30px', marginBottom: '15px', fontSize: '16px', color: '#2c3e50' }}>
                Address
              </h3>

              <div className="form-group">
                <label className="form-label">Street Address</label>
                <input
                  type="text"
                  name="address.street"
                  className="form-input"
                  placeholder="House no., Building name, Street"
                  value={formData.address.street}
                  onChange={handleChange}
                />
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '16px' }}>
                <div className="form-group">
                  <label className="form-label">City *</label>
                  <input
                    type="text"
                    name="address.city"
                    className="form-input"
                    placeholder="City"
                    value={formData.address.city}
                    onChange={handleChange}
                    required
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">State *</label>
                  <input
                    type="text"
                    name="address.state"
                    className="form-input"
                    placeholder="State"
                    value={formData.address.state}
                    onChange={handleChange}
                    required
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Pincode *</label>
                  <input
                    type="text"
                    name="address.pincode"
                    className="form-input"
                    placeholder="6-digit"
                    value={formData.address.pincode}
                    onChange={(e) => {
                      const value = e.target.value.replace(/\D/g, '').slice(0, 6);
                      handleChange({ target: { name: 'address.pincode', value } });
                    }}
                    maxLength="6"
                    required
                  />
                </div>
              </div>
            </div>
          )}

          {/* Step 2 for DP: Service Details */}
          {currentStep === 2 && userRole === 'DP' && (
            <div>
              <h2 style={{ marginBottom: '20px', fontSize: '18px', color: '#2c3e50' }}>
                Service Details
              </h2>

              <div className="form-group">
                <label className="form-label">Vehicle Type *</label>
                <select
                  name="vehicleType"
                  className="form-select"
                  value={formData.vehicleType}
                  onChange={handleChange}
                  required
                >
                  <option value="">Select Vehicle Type</option>
                  <option value="Bike">Bike</option>
                  <option value="Scooter">Scooter</option>
                  <option value="Bicycle">Bicycle</option>
                  <option value="Car">Car</option>
                  <option value="Van">Van</option>
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Languages Known *</label>
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: '10px', marginTop: '8px' }}>
                  {['English', 'Hindi', 'Tamil', 'Telugu', 'Kannada', 'Malayalam', 'Marathi', 'Bengali'].map(lang => (
                    <label key={lang} style={{ display: 'flex', alignItems: 'center', cursor: 'pointer' }}>
                      <input
                        type="checkbox"
                        checked={formData.languagesKnown.includes(lang)}
                        onChange={() => handleLanguageChange(lang)}
                        style={{ marginRight: '5px' }}
                      />
                      {lang}
                    </label>
                  ))}
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group">
                  <label className="form-label">Availability *</label>
                  <select
                    name="availability"
                    className="form-select"
                    value={formData.availability}
                    onChange={handleChange}
                    required
                  >
                    <option value="">Select Availability</option>
                    <option value="Full-time">Full-time</option>
                    <option value="Part-time">Part-time</option>
                    <option value="Weekends">Weekends Only</option>
                  </select>
                </div>

                <div className="form-group">
                  <label className="form-label">Service Area *</label>
                  <input
                    type="text"
                    name="serviceArea"
                    className="form-input"
                    placeholder="e.g., Bangalore Central"
                    value={formData.serviceArea}
                    onChange={handleChange}
                    required
                  />
                </div>
              </div>

              <h3 style={{ marginTop: '30px', marginBottom: '15px', fontSize: '16px', color: '#2c3e50' }}>
                Pricing (in INR)
              </h3>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group">
                  <label className="form-label">Base Rate (per delivery) *</label>
                  <input
                    type="number"
                    name="pricing.baseRate"
                    className="form-input"
                    placeholder="e.g., 30"
                    value={formData.pricing.baseRate}
                    onChange={handleChange}
                    min="0"
                    step="0.01"
                    required
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Per KM Rate *</label>
                  <input
                    type="number"
                    name="pricing.perKmRate"
                    className="form-input"
                    placeholder="e.g., 10"
                    value={formData.pricing.perKmRate}
                    onChange={handleChange}
                    min="0"
                    step="0.01"
                    required
                  />
                </div>
              </div>
            </div>
          )}

          {/* Step 2 for BC: Business Details */}
          {currentStep === 2 && (userRole === 'BC' || userRole === 'DBC') && (
            <div>
              <h2 style={{ marginBottom: '20px', fontSize: '18px', color: '#2c3e50' }}>
                Business Details
              </h2>

              <div className="form-group">
                <label className="form-label">Business Name *</label>
                <input
                  type="text"
                  name="businessName"
                  className="form-input"
                  placeholder="Enter your business name"
                  value={formData.businessName}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="form-group">
                <label className="form-label">Business Type *</label>
                <select
                  name="businessType"
                  className="form-select"
                  value={formData.businessType}
                  onChange={handleChange}
                  required
                >
                  <option value="">Select Business Type</option>
                  <option value="Restaurant">Restaurant</option>
                  <option value="Grocery">Grocery Store</option>
                  <option value="Pharmacy">Pharmacy</option>
                  <option value="E-commerce">E-commerce</option>
                  <option value="Retail">Retail Store</option>
                  <option value="Other">Other</option>
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">GSTIN (Optional)</label>
                <input
                  type="text"
                  name="gstin"
                  className="form-input"
                  placeholder="e.g., 22AAAAA0000A1Z5"
                  value={formData.gstin}
                  onChange={handleChange}
                />
                <p style={{ fontSize: '12px', color: '#666', marginTop: '5px' }}>
                  Enter your GST Identification Number if available
                </p>
              </div>
            </div>
          )}

          {/* Review Step (Last step for all roles) */}
          {currentStep === totalSteps && (
            <div>
              <h2 style={{ marginBottom: '20px', fontSize: '18px', color: '#2c3e50' }}>
                Review Your Information
              </h2>

              <div style={{ backgroundColor: '#f9f9f9', padding: '20px', borderRadius: '8px' }}>
                <h3 style={{ fontSize: '16px', marginBottom: '15px', color: '#2c3e50' }}>Personal Information</h3>
                <p><strong>Name:</strong> {formData.name}</p>
                {formData.dateOfBirth && <p><strong>Date of Birth:</strong> {formData.dateOfBirth}</p>}
                {formData.gender && <p><strong>Gender:</strong> {formData.gender}</p>}
                <p><strong>Email:</strong> {formData.email}</p>
                <p><strong>Address:</strong> {formData.address.street}, {formData.address.city}, {formData.address.state} - {formData.address.pincode}</p>

                {userRole === 'DP' && (
                  <>
                    <h3 style={{ fontSize: '16px', marginTop: '20px', marginBottom: '15px', color: '#2c3e50' }}>Service Details</h3>
                    <p><strong>Vehicle Type:</strong> {formData.vehicleType}</p>
                    <p><strong>Languages:</strong> {formData.languagesKnown.join(', ')}</p>
                    <p><strong>Availability:</strong> {formData.availability}</p>
                    <p><strong>Service Area:</strong> {formData.serviceArea}</p>
                    <p><strong>Pricing:</strong> Base: Rs.{formData.pricing.baseRate}, Per KM: Rs.{formData.pricing.perKmRate}</p>
                  </>
                )}

                {(userRole === 'BC' || userRole === 'DBC') && (
                  <>
                    <h3 style={{ fontSize: '16px', marginTop: '20px', marginBottom: '15px', color: '#2c3e50' }}>Business Details</h3>
                    <p><strong>Business Name:</strong> {formData.businessName}</p>
                    <p><strong>Business Type:</strong> {formData.businessType}</p>
                    {formData.gstin && <p><strong>GSTIN:</strong> {formData.gstin}</p>}
                  </>
                )}
              </div>

              <div className="alert alert-info" style={{ marginTop: '20px' }}>
                Please review all information carefully. You can edit by going back to previous steps.
              </div>
            </div>
          )}

          {loading && <LoadingSpinner message="Submitting your registration..." />}

          {/* Navigation Buttons */}
          <div className="btn-group" style={{ marginTop: '30px' }}>
            {currentStep > 1 && (
              <button
                className="btn btn-secondary"
                onClick={handlePrevious}
                disabled={loading}
              >
                Previous
              </button>
            )}

            {currentStep < totalSteps ? (
              <button
                className="btn btn-primary"
                onClick={handleNext}
                disabled={loading}
              >
                Next
              </button>
            ) : (
              <button
                className="btn btn-primary"
                onClick={handleSubmit}
                disabled={loading}
              >
                {loading ? 'Submitting...' : 'Complete Registration'}
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default RegistrationPage;
