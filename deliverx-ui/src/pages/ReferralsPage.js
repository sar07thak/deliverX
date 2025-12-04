import React, { useState, useEffect } from 'react';
import { getMyReferralCode, applyReferralCode, getReferralStats, getCharities, getDonationStats, makeDonation, getDonationPreferences, updateDonationPreferences } from '../services/referralService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

const ReferralsPage = () => {
  const [activeTab, setActiveTab] = useState('referrals');
  const [referralCode, setReferralCode] = useState(null);
  const [referralStats, setReferralStats] = useState(null);
  const [charities, setCharities] = useState([]);
  const [donationStats, setDonationStats] = useState(null);
  const [donationPrefs, setDonationPrefs] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // Apply code form
  const [applyCode, setApplyCode] = useState('');
  const [applying, setApplying] = useState(false);

  // Donation form
  const [selectedCharity, setSelectedCharity] = useState(null);
  const [donationAmount, setDonationAmount] = useState('');
  const [donating, setDonating] = useState(false);

  useEffect(() => {
    loadData();
  }, [activeTab]);

  const loadData = async () => {
    try {
      setLoading(true);
      if (activeTab === 'referrals') {
        const [codeData, statsData] = await Promise.all([
          getMyReferralCode(),
          getReferralStats()
        ]);
        setReferralCode(codeData);
        setReferralStats(statsData);
      } else {
        const [charitiesData, statsData, prefsData] = await Promise.all([
          getCharities(),
          getDonationStats(),
          getDonationPreferences()
        ]);
        setCharities(charitiesData || []);
        setDonationStats(statsData);
        setDonationPrefs(prefsData);
      }
    } catch (err) {
      setError(err.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  const handleApplyCode = async () => {
    if (!applyCode.trim()) return;
    try {
      setApplying(true);
      const result = await applyReferralCode(applyCode.trim());
      if (result.isSuccess) {
        setSuccess(`${result.message} You received ${formatCurrency(result.bonusAmount)}!`);
        setApplyCode('');
      } else {
        setError(result.message || 'Failed to apply code');
      }
    } catch (err) {
      setError(err.message || 'Failed to apply referral code');
    } finally {
      setApplying(false);
    }
  };

  const handleDonate = async () => {
    if (!selectedCharity || !donationAmount) return;
    try {
      setDonating(true);
      const result = await makeDonation(selectedCharity.id, parseFloat(donationAmount));
      if (result.isSuccess) {
        setSuccess(result.message);
        setSelectedCharity(null);
        setDonationAmount('');
        loadData();
      } else {
        setError(result.message || 'Donation failed');
      }
    } catch (err) {
      setError(err.message || 'Failed to make donation');
    } finally {
      setDonating(false);
    }
  };

  const handleToggleRoundUp = async () => {
    try {
      await updateDonationPreferences(!donationPrefs?.enableRoundUp, donationPrefs?.preferredCharityId, donationPrefs?.monthlyLimit);
      loadData();
    } catch (err) {
      setError(err.message || 'Failed to update preferences');
    }
  };

  const copyToClipboard = (text) => {
    navigator.clipboard.writeText(text);
    setSuccess('Copied to clipboard!');
    setTimeout(() => setSuccess(''), 2000);
  };

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR'
    }).format(amount || 0);
  };

  if (loading) return <LoadingSpinner />;

  return (
    <div style={{ padding: '20px', maxWidth: '1000px', margin: '0 auto' }}>
      <h1>Referrals & Donations</h1>

      {error && <ErrorMessage message={error} onClose={() => setError('')} />}
      {success && (
        <div style={{ background: '#d4edda', color: '#155724', padding: '15px', borderRadius: '8px', marginBottom: '20px' }}>
          {success}
        </div>
      )}

      {/* Tabs */}
      <div style={{ display: 'flex', gap: '10px', marginBottom: '30px' }}>
        <button
          onClick={() => setActiveTab('referrals')}
          style={{
            padding: '12px 24px',
            border: 'none',
            borderRadius: '8px',
            background: activeTab === 'referrals' ? '#667eea' : '#e0e0e0',
            color: activeTab === 'referrals' ? 'white' : '#333',
            cursor: 'pointer',
            fontWeight: '500'
          }}
        >
          Referrals
        </button>
        <button
          onClick={() => setActiveTab('donations')}
          style={{
            padding: '12px 24px',
            border: 'none',
            borderRadius: '8px',
            background: activeTab === 'donations' ? '#667eea' : '#e0e0e0',
            color: activeTab === 'donations' ? 'white' : '#333',
            cursor: 'pointer',
            fontWeight: '500'
          }}
        >
          Donations
        </button>
      </div>

      {activeTab === 'referrals' ? (
        <>
          {/* Referral Code Card */}
          <div style={{
            background: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
            color: 'white',
            padding: '30px',
            borderRadius: '15px',
            marginBottom: '30px',
            textAlign: 'center'
          }}>
            <h2 style={{ margin: '0 0 10px' }}>Your Referral Code</h2>
            <div style={{
              background: 'rgba(255,255,255,0.2)',
              padding: '15px 30px',
              borderRadius: '10px',
              display: 'inline-block',
              marginBottom: '15px'
            }}>
              <span style={{ fontSize: '2em', fontWeight: 'bold', letterSpacing: '3px' }}>
                {referralCode?.code || 'Loading...'}
              </span>
            </div>
            <div>
              <button
                onClick={() => copyToClipboard(referralCode?.code)}
                style={{
                  padding: '10px 20px',
                  background: 'white',
                  color: '#f5576c',
                  border: 'none',
                  borderRadius: '5px',
                  cursor: 'pointer',
                  marginRight: '10px'
                }}
              >
                Copy Code
              </button>
              <button
                onClick={() => copyToClipboard(referralCode?.shareLink)}
                style={{
                  padding: '10px 20px',
                  background: 'rgba(255,255,255,0.2)',
                  color: 'white',
                  border: '1px solid white',
                  borderRadius: '5px',
                  cursor: 'pointer'
                }}
              >
                Copy Link
              </button>
            </div>
            <p style={{ margin: '15px 0 0', opacity: 0.9 }}>
              Earn {formatCurrency(referralCode?.referrerReward)} when your friend completes a delivery!
            </p>
          </div>

          {/* Stats Grid */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '20px', marginBottom: '30px' }}>
            <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', textAlign: 'center' }}>
              <h3 style={{ margin: '0 0 10px', color: '#666' }}>Total Referrals</h3>
              <p style={{ fontSize: '2em', fontWeight: 'bold', margin: 0, color: '#333' }}>{referralStats?.totalReferrals || 0}</p>
            </div>
            <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', textAlign: 'center' }}>
              <h3 style={{ margin: '0 0 10px', color: '#666' }}>Completed</h3>
              <p style={{ fontSize: '2em', fontWeight: 'bold', margin: 0, color: '#28a745' }}>{referralStats?.completedReferrals || 0}</p>
            </div>
            <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', textAlign: 'center' }}>
              <h3 style={{ margin: '0 0 10px', color: '#666' }}>Total Earnings</h3>
              <p style={{ fontSize: '2em', fontWeight: 'bold', margin: 0, color: '#667eea' }}>{formatCurrency(referralStats?.totalEarnings)}</p>
            </div>
          </div>

          {/* Apply Referral Code */}
          <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
            <h3 style={{ marginTop: 0 }}>Have a Referral Code?</h3>
            <div style={{ display: 'flex', gap: '10px' }}>
              <input
                type="text"
                value={applyCode}
                onChange={(e) => setApplyCode(e.target.value.toUpperCase())}
                placeholder="Enter referral code"
                style={{
                  flex: 1,
                  padding: '12px',
                  borderRadius: '5px',
                  border: '1px solid #ddd',
                  fontSize: '1em'
                }}
              />
              <button
                onClick={handleApplyCode}
                disabled={applying || !applyCode.trim()}
                style={{
                  padding: '12px 24px',
                  background: '#667eea',
                  color: 'white',
                  border: 'none',
                  borderRadius: '5px',
                  cursor: 'pointer',
                  fontWeight: '500'
                }}
              >
                {applying ? 'Applying...' : 'Apply'}
              </button>
            </div>
          </div>
        </>
      ) : (
        <>
          {/* Donation Stats */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '20px', marginBottom: '30px' }}>
            <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', textAlign: 'center' }}>
              <h3 style={{ margin: '0 0 10px', color: '#666' }}>Total Donated</h3>
              <p style={{ fontSize: '2em', fontWeight: 'bold', margin: 0, color: '#28a745' }}>{formatCurrency(donationStats?.totalDonated)}</p>
            </div>
            <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', textAlign: 'center' }}>
              <h3 style={{ margin: '0 0 10px', color: '#666' }}>This Month</h3>
              <p style={{ fontSize: '2em', fontWeight: 'bold', margin: 0, color: '#17a2b8' }}>{formatCurrency(donationStats?.thisMonthDonated)}</p>
            </div>
            <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', textAlign: 'center' }}>
              <h3 style={{ margin: '0 0 10px', color: '#666' }}>Donations Made</h3>
              <p style={{ fontSize: '2em', fontWeight: 'bold', margin: 0, color: '#6f42c1' }}>{donationStats?.totalDonations || 0}</p>
            </div>
          </div>

          {/* Round-Up Setting */}
          <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', marginBottom: '30px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <h3 style={{ margin: '0 0 5px' }}>Round-Up Donations</h3>
                <p style={{ margin: 0, color: '#666' }}>Automatically round up your delivery payments and donate the change</p>
              </div>
              <button
                onClick={handleToggleRoundUp}
                style={{
                  padding: '10px 20px',
                  background: donationPrefs?.enableRoundUp ? '#28a745' : '#e0e0e0',
                  color: donationPrefs?.enableRoundUp ? 'white' : '#333',
                  border: 'none',
                  borderRadius: '20px',
                  cursor: 'pointer',
                  fontWeight: '500'
                }}
              >
                {donationPrefs?.enableRoundUp ? 'Enabled' : 'Disabled'}
              </button>
            </div>
          </div>

          {/* Charities */}
          <h2>Support a Charity</h2>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '20px' }}>
            {charities.map((charity) => (
              <div key={charity.id} style={{
                background: 'white',
                borderRadius: '10px',
                boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
                overflow: 'hidden'
              }}>
                <div style={{ padding: '20px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', marginBottom: '15px' }}>
                    <div style={{
                      width: '50px',
                      height: '50px',
                      background: '#e3f2fd',
                      borderRadius: '10px',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      marginRight: '15px',
                      fontSize: '1.5em'
                    }}>
                      ❤️
                    </div>
                    <div>
                      <h3 style={{ margin: 0 }}>{charity.name}</h3>
                      <span style={{ color: '#666', fontSize: '0.9em' }}>{charity.category}</span>
                    </div>
                  </div>
                  <p style={{ color: '#666', margin: '0 0 15px', fontSize: '0.95em' }}>
                    {charity.description}
                  </p>
                  <p style={{ color: '#28a745', margin: '0 0 15px', fontWeight: '500' }}>
                    Total Received: {formatCurrency(charity.totalReceived)}
                  </p>
                  <button
                    onClick={() => setSelectedCharity(charity)}
                    style={{
                      width: '100%',
                      padding: '10px',
                      background: '#667eea',
                      color: 'white',
                      border: 'none',
                      borderRadius: '5px',
                      cursor: 'pointer',
                      fontWeight: '500'
                    }}
                  >
                    Donate Now
                  </button>
                </div>
              </div>
            ))}
          </div>

          {/* Donation Modal */}
          {selectedCharity && (
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
                maxWidth: '400px'
              }}>
                <h2 style={{ marginTop: 0 }}>Donate to {selectedCharity.name}</h2>
                <input
                  type="number"
                  value={donationAmount}
                  onChange={(e) => setDonationAmount(e.target.value)}
                  placeholder="Enter amount"
                  style={{
                    width: '100%',
                    padding: '15px',
                    fontSize: '1.2em',
                    borderRadius: '8px',
                    border: '2px solid #ddd',
                    marginBottom: '20px',
                    boxSizing: 'border-box'
                  }}
                />
                <div style={{ display: 'flex', gap: '10px', marginBottom: '20px' }}>
                  {[100, 500, 1000].map(amt => (
                    <button
                      key={amt}
                      onClick={() => setDonationAmount(amt.toString())}
                      style={{
                        flex: 1,
                        padding: '10px',
                        border: '1px solid #667eea',
                        borderRadius: '5px',
                        background: 'white',
                        color: '#667eea',
                        cursor: 'pointer'
                      }}
                    >
                      {formatCurrency(amt)}
                    </button>
                  ))}
                </div>
                <div style={{ display: 'flex', gap: '10px' }}>
                  <button
                    onClick={() => { setSelectedCharity(null); setDonationAmount(''); }}
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
                    onClick={handleDonate}
                    disabled={donating || !donationAmount}
                    style={{
                      flex: 1,
                      padding: '12px',
                      border: 'none',
                      borderRadius: '8px',
                      background: '#28a745',
                      color: 'white',
                      cursor: 'pointer',
                      fontWeight: '600'
                    }}
                  >
                    {donating ? 'Processing...' : 'Donate'}
                  </button>
                </div>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
};

export default ReferralsPage;
