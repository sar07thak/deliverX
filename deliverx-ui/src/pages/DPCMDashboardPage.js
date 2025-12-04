import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';
import {
  getDPCMDashboard,
  getDPCMPartners,
  updateDPStatus,
  getDPCMDeliveries,
  getDPCMCommissionConfig,
  updateDPCMCommissionConfig,
  getDPCMSettlements,
  requestDPCMSettlement
} from '../services/dpcmService';
import { getServiceAreas } from '../services/serviceAreaService';

/**
 * DPCM (Delivery Partner Channel Manager) Dashboard
 * Manages delivery partners, their performance, and commissions
 */
const DPCMDashboardPage = () => {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState('overview');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // Dashboard data
  const [stats, setStats] = useState({
    totalDPs: 0,
    activeDPs: 0,
    pendingKYC: 0,
    totalDeliveries: 0,
    todayDeliveries: 0,
    totalCommission: 0,
    pendingSettlement: 0
  });

  // Managed DPs list from dashboard
  const [managedDPs, setManagedDPs] = useState([]);
  const [pendingActions, setPendingActions] = useState([]);
  const [earnings, setEarnings] = useState({});

  // Delivery Partners list
  const [deliveryPartners, setDeliveryPartners] = useState([]);
  const [dpPage, setDpPage] = useState(1);
  const [dpTotalPages, setDpTotalPages] = useState(1);
  const [dpFilter, setDpFilter] = useState('all');

  // Deliveries
  const [deliveries, setDeliveries] = useState([]);
  const [deliveryPage, setDeliveryPage] = useState(1);
  const [deliveryTotalPages, setDeliveryTotalPages] = useState(1);
  const [deliveryStatus, setDeliveryStatus] = useState('');

  // Commission config
  const [commissionConfig, setCommissionConfig] = useState({
    commissionType: 'PERCENTAGE',
    commissionValue: 10,
    minCommission: 5,
    maxCommission: 100
  });

  // Service areas
  const [serviceAreas, setServiceAreas] = useState([]);

  // Settlements
  const [settlements, setSettlements] = useState([]);
  const [settlementSummary, setSettlementSummary] = useState({});

  useEffect(() => {
    loadDashboardData();
  }, []);

  useEffect(() => {
    if (activeTab === 'partners') loadDeliveryPartners();
    if (activeTab === 'deliveries') loadDeliveries();
    if (activeTab === 'service-areas') loadServiceAreas();
    if (activeTab === 'commission') loadCommissionConfig();
    if (activeTab === 'settlements') loadSettlements();
  }, [activeTab, dpPage, dpFilter, deliveryPage, deliveryStatus]);

  const loadDashboardData = async () => {
    try {
      setLoading(true);
      const data = await getDPCMDashboard();

      // Map API response to UI state
      setStats({
        totalDPs: data.stats?.totalManagedDPs || 0,
        activeDPs: data.stats?.activeDPs || 0,
        pendingKYC: data.stats?.pendingOnboarding || 0,
        totalDeliveries: data.stats?.totalDeliveries || 0,
        todayDeliveries: data.stats?.deliveriesToday || 0,
        totalCommission: data.earnings?.totalEarnings || 0,
        pendingSettlement: data.earnings?.pendingSettlement || 0
      });

      setManagedDPs(data.managedDPs || []);
      setPendingActions(data.pendingActions || []);
      setEarnings(data.earnings || {});
    } catch (err) {
      setError(err.message || 'Failed to load dashboard');
    } finally {
      setLoading(false);
    }
  };

  const loadDeliveryPartners = async () => {
    try {
      const data = await getDPCMPartners({ status: dpFilter, page: dpPage, pageSize: 10 });
      setDeliveryPartners(data.items || []);
      setDpTotalPages(Math.ceil((data.totalCount || 0) / 10));
    } catch (err) {
      setError(err.message || 'Failed to load delivery partners');
    }
  };

  const loadDeliveries = async () => {
    try {
      const data = await getDPCMDeliveries({ status: deliveryStatus, page: deliveryPage, pageSize: 10 });
      setDeliveries(data.items || []);
      setDeliveryTotalPages(Math.ceil((data.totalCount || 0) / 10));
    } catch (err) {
      setError(err.message || 'Failed to load deliveries');
    }
  };

  const loadServiceAreas = async () => {
    try {
      const response = await getServiceAreas();
      setServiceAreas(response.data || response || []);
    } catch (err) {
      setServiceAreas([]);
    }
  };

  const loadCommissionConfig = async () => {
    try {
      const data = await getDPCMCommissionConfig();
      setCommissionConfig({
        commissionType: data.commissionType || 'PERCENTAGE',
        commissionValue: data.commissionValue || 10,
        minCommission: data.minCommission || 5,
        maxCommission: data.maxCommission || 100
      });
    } catch (err) {
      // Use defaults if not found
    }
  };

  const loadSettlements = async () => {
    try {
      const data = await getDPCMSettlements({ page: 1, pageSize: 20 });
      setSettlements(data.items || []);
      setSettlementSummary(data.summary || {});
    } catch (err) {
      setError(err.message || 'Failed to load settlements');
    }
  };

  const handleActivateDP = async (dpId) => {
    try {
      await updateDPStatus(dpId, true);
      setSuccess('Delivery partner activated successfully');
      loadDeliveryPartners();
      loadDashboardData();
    } catch (err) {
      setError(err.message || 'Failed to activate delivery partner');
    }
  };

  const handleDeactivateDP = async (dpId) => {
    try {
      await updateDPStatus(dpId, false);
      setSuccess('Delivery partner deactivated');
      loadDeliveryPartners();
      loadDashboardData();
    } catch (err) {
      setError(err.message || 'Failed to deactivate delivery partner');
    }
  };

  const handleSaveCommissionConfig = async () => {
    try {
      await updateDPCMCommissionConfig(commissionConfig);
      setSuccess('Commission configuration saved successfully');
    } catch (err) {
      setError(err.message || 'Failed to save commission configuration');
    }
  };

  const handleRequestSettlement = async () => {
    const amount = settlementSummary.availableBalance || 0;
    if (amount < 100) {
      setError('Minimum settlement amount is Rs. 100');
      return;
    }
    try {
      await requestDPCMSettlement(amount);
      setSuccess('Settlement request submitted successfully');
      loadSettlements();
      loadDashboardData();
    } catch (err) {
      setError(err.message || 'Failed to request settlement');
    }
  };

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 0
    }).format(amount || 0);
  };

  const formatDate = (dateString) => {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString('en-IN', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const getStatusColor = (status) => {
    const colors = {
      'ACTIVE': '#28a745',
      'INACTIVE': '#dc3545',
      'PENDING': '#ffc107',
      'DELIVERED': '#28a745',
      'IN_TRANSIT': '#17a2b8',
      'PICKED_UP': '#6f42c1',
      'CREATED': '#6c757d',
      'VERIFIED': '#28a745',
      'APPROVED': '#28a745',
      'REJECTED': '#dc3545',
      'NOT_SUBMITTED': '#6c757d',
      'COMPLETED': '#28a745'
    };
    return colors[status] || '#6c757d';
  };

  const tabs = [
    { id: 'overview', label: 'Overview', icon: '📊' },
    { id: 'partners', label: 'Delivery Partners', icon: '🚴' },
    { id: 'deliveries', label: 'Deliveries', icon: '📦' },
    { id: 'commission', label: 'Commission', icon: '💰' },
    { id: 'service-areas', label: 'Service Areas', icon: '📍' },
    { id: 'settlements', label: 'Settlements', icon: '🏦' }
  ];

  if (loading) return <LoadingSpinner message="Loading DPCM Dashboard..." />;

  return (
    <div style={{ padding: '20px', maxWidth: '1400px', margin: '0 auto' }}>
      {/* Header */}
      <div style={{ marginBottom: '30px' }}>
        <h1 style={{ margin: 0, color: '#2c3e50' }}>DPCM Dashboard</h1>
        <p style={{ color: '#666', margin: '5px 0 0' }}>
          Welcome back, {user?.name || 'Channel Manager'}
        </p>
      </div>

      {error && <ErrorMessage message={error} onClose={() => setError('')} />}
      {success && (
        <div style={{ background: '#d4edda', color: '#155724', padding: '15px', borderRadius: '8px', marginBottom: '20px' }}>
          {success}
          <button onClick={() => setSuccess('')} style={{ float: 'right', background: 'none', border: 'none', cursor: 'pointer', fontSize: '18px' }}>x</button>
        </div>
      )}

      {/* Tabs */}
      <div style={{ display: 'flex', gap: '5px', marginBottom: '20px', borderBottom: '2px solid #e0e0e0', paddingBottom: '10px', overflowX: 'auto' }}>
        {tabs.map(tab => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            style={{
              padding: '10px 16px',
              border: 'none',
              borderRadius: '5px 5px 0 0',
              background: activeTab === tab.id ? '#667eea' : 'transparent',
              color: activeTab === tab.id ? 'white' : '#666',
              cursor: 'pointer',
              fontWeight: activeTab === tab.id ? '600' : '400',
              whiteSpace: 'nowrap',
              display: 'flex',
              alignItems: 'center',
              gap: '6px'
            }}
          >
            <span>{tab.icon}</span>
            {tab.label}
          </button>
        ))}
      </div>

      {/* Overview Tab */}
      {activeTab === 'overview' && (
        <>
          {/* Stats Cards */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '20px', marginBottom: '30px' }}>
            <StatCard title="Total DPs" value={stats.totalDPs} color="#667eea" icon="👥" />
            <StatCard title="Active DPs" value={stats.activeDPs} color="#28a745" icon="✅" />
            <StatCard title="Pending KYC" value={stats.pendingKYC} color="#ffc107" icon="⏳" />
            <StatCard title="Today's Deliveries" value={stats.todayDeliveries} color="#17a2b8" icon="📦" />
            <StatCard title="Total Deliveries" value={stats.totalDeliveries} color="#6f42c1" icon="📊" />
            <StatCard title="Total Commission" value={formatCurrency(stats.totalCommission)} color="#28a745" icon="💰" isText />
            <StatCard title="Pending Settlement" value={formatCurrency(stats.pendingSettlement)} color="#dc3545" icon="🏦" isText />
          </div>

          {/* Quick Actions */}
          <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', marginBottom: '20px' }}>
            <h3 style={{ marginTop: 0 }}>Quick Actions</h3>
            <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
              <button onClick={() => setActiveTab('partners')} className="btn btn-primary">
                View Delivery Partners
              </button>
              <button onClick={() => setActiveTab('deliveries')} className="btn btn-secondary">
                Track Deliveries
              </button>
              <button onClick={() => setActiveTab('settlements')} className="btn btn-secondary">
                Request Settlement
              </button>
            </div>
          </div>

          {/* Pending Actions */}
          {pendingActions.length > 0 && (
            <div style={{ background: '#fff3cd', padding: '20px', borderRadius: '10px', marginBottom: '20px' }}>
              <h3 style={{ marginTop: 0, color: '#856404' }}>Pending Actions</h3>
              {pendingActions.map((action, i) => (
                <p key={i} style={{ margin: '5px 0', color: '#856404' }}>
                  {action.priority === 'HIGH' ? '⚠️' : '📌'} {action.description}
                </p>
              ))}
            </div>
          )}

          {/* Recent Managed DPs */}
          <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
            <h3 style={{ marginTop: 0 }}>Recent Partners</h3>
            {managedDPs.length > 0 ? (
              <div style={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead>
                    <tr style={{ borderBottom: '2px solid #e0e0e0' }}>
                      <th style={{ padding: '10px', textAlign: 'left' }}>Name</th>
                      <th style={{ padding: '10px', textAlign: 'center' }}>Status</th>
                      <th style={{ padding: '10px', textAlign: 'center' }}>Rating</th>
                      <th style={{ padding: '10px', textAlign: 'center' }}>Online</th>
                    </tr>
                  </thead>
                  <tbody>
                    {managedDPs.slice(0, 5).map(dp => (
                      <tr key={dp.dpId} style={{ borderBottom: '1px solid #e0e0e0' }}>
                        <td style={{ padding: '10px' }}>{dp.name}</td>
                        <td style={{ padding: '10px', textAlign: 'center' }}>
                          <span style={{ padding: '3px 8px', borderRadius: '12px', background: getStatusColor(dp.status) + '20', color: getStatusColor(dp.status), fontSize: '0.85em' }}>
                            {dp.status}
                          </span>
                        </td>
                        <td style={{ padding: '10px', textAlign: 'center' }}>
                          {dp.rating > 0 ? `⭐ ${dp.rating.toFixed(1)}` : '-'}
                        </td>
                        <td style={{ padding: '10px', textAlign: 'center' }}>
                          {dp.isOnline ? '🟢' : '⚫'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p style={{ color: '#666' }}>No delivery partners yet. DPs linked to your account will appear here.</p>
            )}
          </div>
        </>
      )}

      {/* Delivery Partners Tab */}
      {activeTab === 'partners' && (
        <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px', flexWrap: 'wrap', gap: '10px' }}>
            <h2 style={{ margin: 0 }}>My Delivery Partners</h2>
            <select
              value={dpFilter}
              onChange={(e) => { setDpFilter(e.target.value); setDpPage(1); }}
              style={{ padding: '8px 15px', borderRadius: '5px', border: '1px solid #ddd' }}
            >
              <option value="all">All Partners</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
              <option value="pending-kyc">Pending KYC</option>
            </select>
          </div>

          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: '800px' }}>
              <thead>
                <tr style={{ borderBottom: '2px solid #e0e0e0' }}>
                  <th style={{ padding: '12px', textAlign: 'left' }}>Name</th>
                  <th style={{ padding: '12px', textAlign: 'left' }}>Phone</th>
                  <th style={{ padding: '12px', textAlign: 'center' }}>Status</th>
                  <th style={{ padding: '12px', textAlign: 'center' }}>KYC</th>
                  <th style={{ padding: '12px', textAlign: 'center' }}>Deliveries</th>
                  <th style={{ padding: '12px', textAlign: 'center' }}>Rating</th>
                  <th style={{ padding: '12px', textAlign: 'right' }}>Earnings</th>
                  <th style={{ padding: '12px', textAlign: 'center' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {deliveryPartners.map(dp => (
                  <tr key={dp.id} style={{ borderBottom: '1px solid #e0e0e0' }}>
                    <td style={{ padding: '12px', fontWeight: '500' }}>{dp.name}</td>
                    <td style={{ padding: '12px', color: '#666' }}>{dp.phone}</td>
                    <td style={{ padding: '12px', textAlign: 'center' }}>
                      <span style={{
                        padding: '4px 10px',
                        borderRadius: '20px',
                        background: getStatusColor(dp.status) + '20',
                        color: getStatusColor(dp.status),
                        fontSize: '0.85em',
                        fontWeight: '500'
                      }}>
                        {dp.status}
                      </span>
                    </td>
                    <td style={{ padding: '12px', textAlign: 'center' }}>
                      <span style={{
                        padding: '4px 10px',
                        borderRadius: '20px',
                        background: getStatusColor(dp.kycStatus) + '20',
                        color: getStatusColor(dp.kycStatus),
                        fontSize: '0.85em',
                        fontWeight: '500'
                      }}>
                        {dp.kycStatus || 'NOT_SUBMITTED'}
                      </span>
                    </td>
                    <td style={{ padding: '12px', textAlign: 'center' }}>{dp.totalDeliveries}</td>
                    <td style={{ padding: '12px', textAlign: 'center' }}>
                      {dp.rating > 0 ? `⭐ ${dp.rating.toFixed(1)}` : '-'}
                    </td>
                    <td style={{ padding: '12px', textAlign: 'right', fontWeight: '500', color: '#28a745' }}>
                      {formatCurrency(dp.earnings)}
                    </td>
                    <td style={{ padding: '12px', textAlign: 'center' }}>
                      {dp.status === 'ACTIVE' ? (
                        <button
                          onClick={() => handleDeactivateDP(dp.id)}
                          style={{ padding: '5px 10px', background: '#dc3545', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontSize: '0.85em' }}
                        >
                          Deactivate
                        </button>
                      ) : (
                        <button
                          onClick={() => handleActivateDP(dp.id)}
                          style={{ padding: '5px 10px', background: '#28a745', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontSize: '0.85em' }}
                        >
                          Activate
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {deliveryPartners.length === 0 && (
            <p style={{ textAlign: 'center', color: '#666', padding: '40px' }}>No delivery partners found. DPs registered under your referral will appear here.</p>
          )}

          {/* Pagination */}
          {dpTotalPages > 1 && (
            <div style={{ display: 'flex', justifyContent: 'center', gap: '10px', marginTop: '20px' }}>
              <button disabled={dpPage === 1} onClick={() => setDpPage(dpPage - 1)} className="btn btn-secondary">Previous</button>
              <span style={{ padding: '8px' }}>Page {dpPage} of {dpTotalPages}</span>
              <button disabled={dpPage === dpTotalPages} onClick={() => setDpPage(dpPage + 1)} className="btn btn-secondary">Next</button>
            </div>
          )}
        </div>
      )}

      {/* Deliveries Tab */}
      {activeTab === 'deliveries' && (
        <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px', flexWrap: 'wrap', gap: '10px' }}>
            <h2 style={{ margin: 0 }}>Deliveries by My Partners</h2>
            <select
              value={deliveryStatus}
              onChange={(e) => { setDeliveryStatus(e.target.value); setDeliveryPage(1); }}
              style={{ padding: '8px 15px', borderRadius: '5px', border: '1px solid #ddd' }}
            >
              <option value="">All Status</option>
              <option value="CREATED">Created</option>
              <option value="PICKED_UP">Picked Up</option>
              <option value="IN_TRANSIT">In Transit</option>
              <option value="DELIVERED">Delivered</option>
            </select>
          </div>

          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: '700px' }}>
              <thead>
                <tr style={{ borderBottom: '2px solid #e0e0e0' }}>
                  <th style={{ padding: '12px', textAlign: 'left' }}>Tracking ID</th>
                  <th style={{ padding: '12px', textAlign: 'left' }}>Delivery Partner</th>
                  <th style={{ padding: '12px', textAlign: 'center' }}>Status</th>
                  <th style={{ padding: '12px', textAlign: 'left' }}>Route</th>
                  <th style={{ padding: '12px', textAlign: 'right' }}>Amount</th>
                  <th style={{ padding: '12px', textAlign: 'right' }}>Commission</th>
                </tr>
              </thead>
              <tbody>
                {deliveries.map(delivery => (
                  <tr key={delivery.id} style={{ borderBottom: '1px solid #e0e0e0' }}>
                    <td style={{ padding: '12px', fontWeight: '500', color: '#667eea' }}>{delivery.trackingId}</td>
                    <td style={{ padding: '12px' }}>{delivery.dpName}</td>
                    <td style={{ padding: '12px', textAlign: 'center' }}>
                      <span style={{
                        padding: '4px 10px',
                        borderRadius: '20px',
                        background: getStatusColor(delivery.status) + '20',
                        color: getStatusColor(delivery.status),
                        fontSize: '0.85em',
                        fontWeight: '500'
                      }}>
                        {delivery.status.replace('_', ' ')}
                      </span>
                    </td>
                    <td style={{ padding: '12px', fontSize: '0.9em', color: '#666' }}>
                      {delivery.pickupAddress ? `${delivery.pickupAddress.substring(0, 20)}...` : '-'} → {delivery.dropAddress ? `${delivery.dropAddress.substring(0, 20)}...` : '-'}
                    </td>
                    <td style={{ padding: '12px', textAlign: 'right' }}>{formatCurrency(delivery.amount)}</td>
                    <td style={{ padding: '12px', textAlign: 'right', color: '#28a745', fontWeight: '500' }}>
                      {formatCurrency(delivery.commission)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {deliveries.length === 0 && (
            <p style={{ textAlign: 'center', color: '#666', padding: '40px' }}>No deliveries found. Deliveries completed by your partners will appear here.</p>
          )}

          {/* Pagination */}
          {deliveryTotalPages > 1 && (
            <div style={{ display: 'flex', justifyContent: 'center', gap: '10px', marginTop: '20px' }}>
              <button disabled={deliveryPage === 1} onClick={() => setDeliveryPage(deliveryPage - 1)} className="btn btn-secondary">Previous</button>
              <span style={{ padding: '8px' }}>Page {deliveryPage} of {deliveryTotalPages}</span>
              <button disabled={deliveryPage === deliveryTotalPages} onClick={() => setDeliveryPage(deliveryPage + 1)} className="btn btn-secondary">Next</button>
            </div>
          )}
        </div>
      )}

      {/* Commission Tab */}
      {activeTab === 'commission' && (
        <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
          <h2 style={{ marginTop: 0 }}>Commission Configuration</h2>
          <p style={{ color: '#666', marginBottom: '20px' }}>
            Configure how you earn commission from your delivery partners
          </p>

          <div style={{ maxWidth: '500px' }}>
            <div className="form-group">
              <label className="form-label">Commission Type</label>
              <select
                className="form-select"
                value={commissionConfig.commissionType}
                onChange={(e) => setCommissionConfig({ ...commissionConfig, commissionType: e.target.value })}
              >
                <option value="PERCENTAGE">Percentage of Delivery Amount</option>
                <option value="FLAT_PER_DELIVERY">Fixed Amount per Delivery</option>
              </select>
            </div>

            <div className="form-group">
              <label className="form-label">
                {commissionConfig.commissionType === 'PERCENTAGE' ? 'Commission Percentage (%)' : 'Fixed Commission Amount (Rs.)'}
              </label>
              <input
                type="number"
                className="form-input"
                value={commissionConfig.commissionValue}
                onChange={(e) => setCommissionConfig({ ...commissionConfig, commissionValue: parseFloat(e.target.value) || 0 })}
                min="0"
                step={commissionConfig.commissionType === 'PERCENTAGE' ? '0.5' : '1'}
              />
            </div>

            {commissionConfig.commissionType === 'PERCENTAGE' && (
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
                <div className="form-group">
                  <label className="form-label">Min Commission (Rs.)</label>
                  <input
                    type="number"
                    className="form-input"
                    value={commissionConfig.minCommission}
                    onChange={(e) => setCommissionConfig({ ...commissionConfig, minCommission: parseFloat(e.target.value) || 0 })}
                    min="0"
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">Max Commission (Rs.)</label>
                  <input
                    type="number"
                    className="form-input"
                    value={commissionConfig.maxCommission}
                    onChange={(e) => setCommissionConfig({ ...commissionConfig, maxCommission: parseFloat(e.target.value) || 0 })}
                    min="0"
                  />
                </div>
              </div>
            )}

            <button onClick={handleSaveCommissionConfig} className="btn btn-primary" style={{ marginTop: '10px' }}>
              Save Configuration
            </button>
          </div>

          {/* Commission Summary */}
          <div style={{ marginTop: '30px', padding: '20px', backgroundColor: '#f8f9fa', borderRadius: '8px' }}>
            <h3 style={{ marginTop: 0 }}>Commission Summary</h3>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '20px' }}>
              <div>
                <p style={{ color: '#666', margin: 0 }}>This Month</p>
                <h3 style={{ margin: '5px 0', color: '#28a745' }}>{formatCurrency(earnings.earningsThisMonth || 0)}</h3>
              </div>
              <div>
                <p style={{ color: '#666', margin: 0 }}>Pending Settlement</p>
                <h3 style={{ margin: '5px 0', color: '#dc3545' }}>{formatCurrency(earnings.pendingSettlement || 0)}</h3>
              </div>
              <div>
                <p style={{ color: '#666', margin: 0 }}>Total Earnings</p>
                <h3 style={{ margin: '5px 0', color: '#17a2b8' }}>{formatCurrency(earnings.totalEarnings || 0)}</h3>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Service Areas Tab */}
      {activeTab === 'service-areas' && (
        <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
          <h2 style={{ marginTop: 0 }}>Service Areas</h2>
          <p style={{ color: '#666', marginBottom: '20px' }}>
            Areas where your delivery partners can operate
          </p>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '20px' }}>
            {serviceAreas.map(area => (
              <div key={area.id} style={{
                padding: '20px',
                border: '1px solid #e0e0e0',
                borderRadius: '8px',
                backgroundColor: area.isActive ? '#fff' : '#f8f9fa'
              }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                  <h3 style={{ margin: 0 }}>{area.areaName || area.name || 'Service Area'}</h3>
                  <span style={{
                    padding: '4px 10px',
                    borderRadius: '20px',
                    background: area.isActive ? '#d4edda' : '#f8d7da',
                    color: area.isActive ? '#155724' : '#721c24',
                    fontSize: '0.8em'
                  }}>
                    {area.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
                <p style={{ color: '#666', margin: '5px 0', fontSize: '0.9em' }}>
                  📍 Center: {(area.centerLat || area.latitude)?.toFixed(4)}, {(area.centerLng || area.longitude)?.toFixed(4)}
                </p>
                <p style={{ color: '#666', margin: '5px 0', fontSize: '0.9em' }}>
                  📏 Radius: {area.radiusKm || area.radius} km
                </p>
              </div>
            ))}
          </div>

          {serviceAreas.length === 0 && (
            <p style={{ textAlign: 'center', color: '#666', padding: '40px' }}>No service areas configured</p>
          )}
        </div>
      )}

      {/* Settlements Tab */}
      {activeTab === 'settlements' && (
        <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
          <h2 style={{ marginTop: 0 }}>Settlements</h2>

          {/* Settlement Summary */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '20px', marginBottom: '30px' }}>
            <div style={{ padding: '20px', backgroundColor: '#d4edda', borderRadius: '8px' }}>
              <p style={{ color: '#155724', margin: 0, fontSize: '0.9em' }}>Available for Settlement</p>
              <h2 style={{ margin: '10px 0 0', color: '#155724' }}>{formatCurrency(settlementSummary.availableBalance || 0)}</h2>
            </div>
            <div style={{ padding: '20px', backgroundColor: '#cce5ff', borderRadius: '8px' }}>
              <p style={{ color: '#004085', margin: 0, fontSize: '0.9em' }}>Total Settled (This Month)</p>
              <h2 style={{ margin: '10px 0 0', color: '#004085' }}>{formatCurrency(settlementSummary.totalSettledThisMonth || 0)}</h2>
            </div>
            <div style={{ padding: '20px', backgroundColor: '#f8f9fa', borderRadius: '8px' }}>
              <p style={{ color: '#666', margin: 0, fontSize: '0.9em' }}>Bank Account</p>
              <h3 style={{ margin: '10px 0 0', color: '#333' }}>{settlementSummary.bankAccount || 'Not linked'}</h3>
            </div>
          </div>

          <button
            className="btn btn-primary"
            disabled={(settlementSummary.availableBalance || 0) < 100}
            onClick={handleRequestSettlement}
          >
            Request Settlement
          </button>
          <p style={{ color: '#666', fontSize: '0.85em', marginTop: '10px' }}>
            Minimum settlement amount: Rs. 100
          </p>

          {/* Settlement History */}
          <h3 style={{ marginTop: '30px' }}>Settlement History</h3>
          {settlements.length > 0 ? (
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ borderBottom: '2px solid #e0e0e0' }}>
                  <th style={{ padding: '12px', textAlign: 'left' }}>Date</th>
                  <th style={{ padding: '12px', textAlign: 'right' }}>Amount</th>
                  <th style={{ padding: '12px', textAlign: 'center' }}>Status</th>
                  <th style={{ padding: '12px', textAlign: 'left' }}>Reference</th>
                </tr>
              </thead>
              <tbody>
                {settlements.map(s => (
                  <tr key={s.id} style={{ borderBottom: '1px solid #e0e0e0' }}>
                    <td style={{ padding: '12px' }}>{formatDate(s.createdAt)}</td>
                    <td style={{ padding: '12px', textAlign: 'right' }}>{formatCurrency(s.amount)}</td>
                    <td style={{ padding: '12px', textAlign: 'center' }}>
                      <span style={{ padding: '4px 10px', borderRadius: '20px', background: getStatusColor(s.status) + '20', color: getStatusColor(s.status), fontSize: '0.85em' }}>
                        {s.status}
                      </span>
                    </td>
                    <td style={{ padding: '12px', color: '#666', fontFamily: 'monospace' }}>{s.referenceId || '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p style={{ color: '#666', textAlign: 'center', padding: '20px' }}>No settlement history yet</p>
          )}
        </div>
      )}
    </div>
  );
};

// Stat Card Component
const StatCard = ({ title, value, color, icon, isText = false }) => (
  <div style={{
    background: 'white',
    padding: '20px',
    borderRadius: '10px',
    boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
    borderLeft: `4px solid ${color}`
  }}>
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
      <div>
        <h4 style={{ margin: '0 0 10px', color: '#666', fontSize: '0.85em', fontWeight: '500' }}>{title}</h4>
        <div style={{ fontSize: isText ? '1.3em' : '1.8em', fontWeight: 'bold', color }}>
          {value}
        </div>
      </div>
      <span style={{ fontSize: '1.5em' }}>{icon}</span>
    </div>
  </div>
);

export default DPCMDashboardPage;
