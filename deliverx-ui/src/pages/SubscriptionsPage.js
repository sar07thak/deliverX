import React, { useState, useEffect } from 'react';
import { getPlans, getMySubscription, subscribe, cancelSubscription, toggleAutoRenew, validatePromoCode } from '../services/subscriptionService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

const SubscriptionsPage = () => {
  const [plans, setPlans] = useState([]);
  const [subscription, setSubscription] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [selectedPlan, setSelectedPlan] = useState(null);
  const [promoCode, setPromoCode] = useState('');
  const [promoDiscount, setPromoDiscount] = useState(null);
  const [subscribing, setSubscribing] = useState(false);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [plansData, subData] = await Promise.all([
        getPlans(),
        getMySubscription()
      ]);
      setPlans(plansData || []);
      setSubscription(subData?.message ? null : subData);
    } catch (err) {
      setError(err.message || 'Failed to load subscription data');
    } finally {
      setLoading(false);
    }
  };

  const handleValidatePromo = async () => {
    if (!promoCode || !selectedPlan) return;
    try {
      const result = await validatePromoCode(promoCode, selectedPlan.id);
      if (result.isValid) {
        setPromoDiscount(result);
        setSuccess(`Promo code applied! ${result.discountPercent}% off`);
      } else {
        setError(result.message || 'Invalid promo code');
        setPromoDiscount(null);
      }
    } catch (err) {
      setError(err.message || 'Failed to validate promo code');
    }
  };

  const handleSubscribe = async (plan) => {
    try {
      setSubscribing(true);
      setError('');
      await subscribe(plan.id, 'WALLET', promoCode || null);
      setSuccess('Successfully subscribed!');
      setSelectedPlan(null);
      setPromoCode('');
      setPromoDiscount(null);
      loadData();
    } catch (err) {
      setError(err.message || 'Failed to subscribe');
    } finally {
      setSubscribing(false);
    }
  };

  const handleCancel = async () => {
    if (!window.confirm('Are you sure you want to cancel your subscription?')) return;
    try {
      await cancelSubscription();
      setSuccess('Subscription cancelled');
      loadData();
    } catch (err) {
      setError(err.message || 'Failed to cancel subscription');
    }
  };

  const handleToggleAutoRenew = async () => {
    try {
      await toggleAutoRenew(!subscription.autoRenew);
      loadData();
    } catch (err) {
      setError(err.message || 'Failed to update auto-renew');
    }
  };

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR'
    }).format(amount || 0);
  };

  if (loading) return <LoadingSpinner />;

  return (
    <div style={{ padding: '20px', maxWidth: '1200px', margin: '0 auto' }}>
      <h1>Subscriptions</h1>

      {error && <ErrorMessage message={error} onClose={() => setError('')} />}
      {success && (
        <div style={{ background: '#d4edda', color: '#155724', padding: '15px', borderRadius: '8px', marginBottom: '20px' }}>
          {success}
        </div>
      )}

      {/* Current Subscription */}
      {subscription && (
        <div style={{
          background: 'linear-gradient(135deg, #11998e 0%, #38ef7d 100%)',
          color: 'white',
          padding: '30px',
          borderRadius: '15px',
          marginBottom: '30px'
        }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <div>
              <p style={{ margin: '0 0 5px', opacity: 0.8 }}>Current Plan</p>
              <h2 style={{ margin: '0 0 15px', fontSize: '1.8em' }}>{subscription.planName}</h2>
              <p style={{ margin: 0 }}>
                <strong>Status:</strong> {subscription.status}
              </p>
              <p style={{ margin: '5px 0' }}>
                <strong>Expires:</strong> {new Date(subscription.endDate).toLocaleDateString()}
              </p>
              <p style={{ margin: '5px 0' }}>
                <strong>Auto-Renew:</strong> {subscription.autoRenew ? 'Enabled' : 'Disabled'}
              </p>
            </div>
            <div style={{ textAlign: 'right' }}>
              <button
                onClick={handleToggleAutoRenew}
                style={{
                  padding: '10px 20px',
                  background: 'rgba(255,255,255,0.2)',
                  color: 'white',
                  border: '1px solid white',
                  borderRadius: '5px',
                  cursor: 'pointer',
                  marginBottom: '10px',
                  display: 'block',
                  width: '100%'
                }}
              >
                {subscription.autoRenew ? 'Disable' : 'Enable'} Auto-Renew
              </button>
              <button
                onClick={handleCancel}
                style={{
                  padding: '10px 20px',
                  background: 'rgba(255,255,255,0.2)',
                  color: 'white',
                  border: '1px solid white',
                  borderRadius: '5px',
                  cursor: 'pointer'
                }}
              >
                Cancel Subscription
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Available Plans */}
      <h2>Available Plans</h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '20px' }}>
        {plans.map((plan) => (
          <div
            key={plan.id}
            style={{
              background: 'white',
              borderRadius: '15px',
              boxShadow: '0 4px 20px rgba(0,0,0,0.1)',
              overflow: 'hidden',
              border: subscription?.planId === plan.id ? '3px solid #11998e' : 'none'
            }}
          >
            <div style={{
              background: plan.isPopular ? 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)' : '#f8f9fa',
              color: plan.isPopular ? 'white' : '#333',
              padding: '20px',
              textAlign: 'center'
            }}>
              {plan.isPopular && (
                <span style={{
                  background: '#ffc107',
                  color: '#333',
                  padding: '4px 12px',
                  borderRadius: '20px',
                  fontSize: '0.8em',
                  fontWeight: '600',
                  marginBottom: '10px',
                  display: 'inline-block'
                }}>
                  MOST POPULAR
                </span>
              )}
              <h3 style={{ margin: '10px 0 5px' }}>{plan.name}</h3>
              <p style={{ margin: 0, opacity: 0.8 }}>{plan.description}</p>
            </div>

            <div style={{ padding: '20px' }}>
              <div style={{ textAlign: 'center', marginBottom: '20px' }}>
                <span style={{ fontSize: '2.5em', fontWeight: 'bold' }}>
                  {formatCurrency(plan.price)}
                </span>
                <span style={{ color: '#666' }}>/{plan.durationDays} days</span>
              </div>

              <ul style={{ listStyle: 'none', padding: 0, margin: '0 0 20px' }}>
                {plan.features && JSON.parse(plan.features).map((feature, i) => (
                  <li key={i} style={{ padding: '8px 0', borderBottom: '1px solid #f0f0f0', display: 'flex', alignItems: 'center' }}>
                    <span style={{ color: '#28a745', marginRight: '10px' }}>✓</span>
                    {feature}
                  </li>
                ))}
              </ul>

              <button
                onClick={() => setSelectedPlan(plan)}
                disabled={subscription?.planId === plan.id}
                style={{
                  width: '100%',
                  padding: '12px',
                  border: 'none',
                  borderRadius: '8px',
                  background: subscription?.planId === plan.id ? '#ccc' : '#667eea',
                  color: 'white',
                  fontWeight: '600',
                  cursor: subscription?.planId === plan.id ? 'default' : 'pointer'
                }}
              >
                {subscription?.planId === plan.id ? 'Current Plan' : 'Subscribe'}
              </button>
            </div>
          </div>
        ))}
      </div>

      {/* Subscribe Modal */}
      {selectedPlan && (
        <div style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: 'rgba(0,0,0,0.5)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1000
        }}>
          <div style={{
            background: 'white',
            padding: '30px',
            borderRadius: '15px',
            width: '90%',
            maxWidth: '450px'
          }}>
            <h2 style={{ marginTop: 0 }}>Subscribe to {selectedPlan.name}</h2>

            <div style={{ background: '#f8f9fa', padding: '15px', borderRadius: '8px', marginBottom: '20px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '10px' }}>
                <span>Plan Price:</span>
                <span>{formatCurrency(selectedPlan.price)}</span>
              </div>
              {promoDiscount && (
                <div style={{ display: 'flex', justifyContent: 'space-between', color: '#28a745' }}>
                  <span>Discount ({promoDiscount.discountPercent}%):</span>
                  <span>-{formatCurrency(selectedPlan.price * promoDiscount.discountPercent / 100)}</span>
                </div>
              )}
              <div style={{ display: 'flex', justifyContent: 'space-between', fontWeight: 'bold', borderTop: '1px solid #ddd', paddingTop: '10px', marginTop: '10px' }}>
                <span>Total:</span>
                <span>
                  {formatCurrency(promoDiscount
                    ? selectedPlan.price * (1 - promoDiscount.discountPercent / 100)
                    : selectedPlan.price
                  )}
                </span>
              </div>
            </div>

            <div style={{ marginBottom: '20px' }}>
              <label style={{ display: 'block', marginBottom: '5px', fontWeight: '500' }}>Promo Code (optional)</label>
              <div style={{ display: 'flex', gap: '10px' }}>
                <input
                  type="text"
                  value={promoCode}
                  onChange={(e) => setPromoCode(e.target.value.toUpperCase())}
                  placeholder="Enter promo code"
                  style={{
                    flex: 1,
                    padding: '10px',
                    borderRadius: '5px',
                    border: '1px solid #ddd'
                  }}
                />
                <button
                  onClick={handleValidatePromo}
                  style={{
                    padding: '10px 15px',
                    border: '1px solid #667eea',
                    borderRadius: '5px',
                    background: 'white',
                    color: '#667eea',
                    cursor: 'pointer'
                  }}
                >
                  Apply
                </button>
              </div>
            </div>

            <div style={{ display: 'flex', gap: '10px' }}>
              <button
                onClick={() => { setSelectedPlan(null); setPromoCode(''); setPromoDiscount(null); }}
                style={{
                  flex: 1,
                  padding: '12px',
                  border: '1px solid #ddd',
                  borderRadius: '8px',
                  background: 'white',
                  cursor: 'pointer'
                }}
              >
                Cancel
              </button>
              <button
                onClick={() => handleSubscribe(selectedPlan)}
                disabled={subscribing}
                style={{
                  flex: 1,
                  padding: '12px',
                  border: 'none',
                  borderRadius: '8px',
                  background: '#667eea',
                  color: 'white',
                  cursor: 'pointer',
                  fontWeight: '600'
                }}
              >
                {subscribing ? 'Processing...' : 'Subscribe Now'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default SubscriptionsPage;
