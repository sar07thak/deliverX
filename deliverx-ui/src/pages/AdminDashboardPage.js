import React, { useState, useEffect } from 'react';
import { getAdminDashboard, getUsers, updateUserStatus, getKYCRequests, approveKYC, rejectKYC, getAuditLogs } from '../services/adminService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

const AdminDashboardPage = () => {
  const [dashboard, setDashboard] = useState(null);
  const [activeTab, setActiveTab] = useState('overview');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // Users state
  const [users, setUsers] = useState([]);
  const [usersPage, setUsersPage] = useState(1);
  const [usersTotalPages, setUsersTotalPages] = useState(1);
  const [userRoleFilter, setUserRoleFilter] = useState('');

  // KYC state
  const [kycRequests, setKYCRequests] = useState([]);
  const [kycPage, setKYCPage] = useState(1);
  const [kycTotalPages, setKYCTotalPages] = useState(1);
  const [kycStatusFilter, setKYCStatusFilter] = useState('PENDING');

  // Audit logs state
  const [auditLogs, setAuditLogs] = useState([]);
  const [logsPage, setLogsPage] = useState(1);
  const [logsTotalPages, setLogsTotalPages] = useState(1);

  useEffect(() => {
    loadDashboard();
  }, []);

  useEffect(() => {
    if (activeTab === 'users') loadUsers();
    if (activeTab === 'kyc') loadKYC();
    if (activeTab === 'audit') loadAuditLogs();
  }, [activeTab, usersPage, userRoleFilter, kycPage, kycStatusFilter, logsPage]);

  const loadDashboard = async () => {
    try {
      setLoading(true);
      const data = await getAdminDashboard();
      setDashboard(data);
    } catch (err) {
      setError(err.message || 'Failed to load dashboard');
    } finally {
      setLoading(false);
    }
  };

  const loadUsers = async () => {
    try {
      const data = await getUsers({ page: usersPage, pageSize: 10, role: userRoleFilter || undefined });
      setUsers(data.items || []);
      setUsersTotalPages(data.totalPages || 1);
    } catch (err) {
      setError(err.message || 'Failed to load users');
    }
  };

  const loadKYC = async () => {
    try {
      const data = await getKYCRequests({ page: kycPage, pageSize: 10, status: kycStatusFilter || undefined });
      setKYCRequests(data.items || []);
      setKYCTotalPages(data.totalPages || 1);
    } catch (err) {
      setError(err.message || 'Failed to load KYC requests');
    }
  };

  const loadAuditLogs = async () => {
    try {
      const data = await getAuditLogs({ page: logsPage, pageSize: 20 });
      setAuditLogs(data.items || []);
      setLogsTotalPages(data.totalPages || 1);
    } catch (err) {
      setError(err.message || 'Failed to load audit logs');
    }
  };

  const handleUserStatusChange = async (userId, isActive) => {
    try {
      await updateUserStatus(userId, isActive ? 'ACTIVE' : 'INACTIVE');
      setSuccess(`User ${isActive ? 'activated' : 'deactivated'} successfully`);
      loadUsers();
    } catch (err) {
      setError(err.message || 'Failed to update user status');
    }
  };

  const handleKYCApprove = async (requestId) => {
    try {
      await approveKYC(requestId);
      setSuccess('KYC approved successfully');
      loadKYC();
    } catch (err) {
      setError(err.message || 'Failed to approve KYC');
    }
  };

  const handleKYCReject = async (requestId) => {
    const reason = window.prompt('Enter rejection reason:');
    if (!reason) return;
    try {
      await rejectKYC(requestId, reason);
      setSuccess('KYC rejected');
      loadKYC();
    } catch (err) {
      setError(err.message || 'Failed to reject KYC');
    }
  };

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR'
    }).format(amount || 0);
  };

  const tabs = [
    { id: 'overview', label: 'Overview' },
    { id: 'users', label: 'Users' },
    { id: 'kyc', label: 'KYC Requests' },
    { id: 'audit', label: 'Audit Logs' }
  ];

  if (loading) return <LoadingSpinner />;

  return (
    <div style={{ padding: '20px', maxWidth: '1400px', margin: '0 auto' }}>
      <h1>Admin Dashboard</h1>

      {error && <ErrorMessage message={error} onClose={() => setError('')} />}
      {success && (
        <div style={{ background: '#d4edda', color: '#155724', padding: '15px', borderRadius: '8px', marginBottom: '20px' }}>
          {success}
          <button onClick={() => setSuccess('')} style={{ float: 'right', background: 'none', border: 'none', cursor: 'pointer' }}>×</button>
        </div>
      )}

      {/* Tabs */}
      <div style={{ display: 'flex', gap: '10px', marginBottom: '20px', borderBottom: '2px solid #e0e0e0', paddingBottom: '10px' }}>
        {tabs.map(tab => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            style={{
              padding: '10px 20px',
              border: 'none',
              borderRadius: '5px 5px 0 0',
              background: activeTab === tab.id ? '#667eea' : 'transparent',
              color: activeTab === tab.id ? 'white' : '#666',
              cursor: 'pointer',
              fontWeight: activeTab === tab.id ? '600' : '400'
            }}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Overview Tab */}
      {activeTab === 'overview' && dashboard && (
        <>
          {/* Stats Cards */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '20px', marginBottom: '30px' }}>
            <StatCard title="Total Users" value={dashboard.platformStats?.totalUsers || 0} color="#667eea" />
            <StatCard title="Total Deliveries" value={dashboard.platformStats?.totalDeliveries || 0} color="#17a2b8" />
            <StatCard title="Active DPs" value={dashboard.platformStats?.activeDPs || 0} color="#28a745" />
            <StatCard title="Pending KYC" value={dashboard.platformStats?.pendingKYC || 0} color="#ffc107" />
          </div>

          {/* Revenue Stats */}
          <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)', marginBottom: '20px' }}>
            <h2 style={{ marginTop: 0 }}>Revenue Overview</h2>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '20px' }}>
              <div>
                <p style={{ color: '#666', margin: 0 }}>Today's Revenue</p>
                <h3 style={{ margin: '5px 0', color: '#28a745' }}>{formatCurrency(dashboard.revenueStats?.todayRevenue)}</h3>
              </div>
              <div>
                <p style={{ color: '#666', margin: 0 }}>This Week</p>
                <h3 style={{ margin: '5px 0', color: '#17a2b8' }}>{formatCurrency(dashboard.revenueStats?.weekRevenue)}</h3>
              </div>
              <div>
                <p style={{ color: '#666', margin: 0 }}>This Month</p>
                <h3 style={{ margin: '5px 0', color: '#667eea' }}>{formatCurrency(dashboard.revenueStats?.monthRevenue)}</h3>
              </div>
              <div>
                <p style={{ color: '#666', margin: 0 }}>Total Platform Fees</p>
                <h3 style={{ margin: '5px 0', color: '#6f42c1' }}>{formatCurrency(dashboard.revenueStats?.totalPlatformFees)}</h3>
              </div>
            </div>
          </div>

          {/* System Alerts */}
          {dashboard.systemAlerts && dashboard.systemAlerts.length > 0 && (
            <div style={{ background: '#fff3cd', padding: '20px', borderRadius: '10px', marginBottom: '20px' }}>
              <h3 style={{ margin: '0 0 15px', color: '#856404' }}>⚠️ System Alerts</h3>
              {dashboard.systemAlerts.map((alert, i) => (
                <div key={i} style={{ padding: '10px', background: 'rgba(255,255,255,0.5)', borderRadius: '5px', marginBottom: '10px' }}>
                  <strong>{alert.type}</strong>: {alert.message}
                </div>
              ))}
            </div>
          )}

          {/* Recent Activity */}
          <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
            <h2 style={{ marginTop: 0 }}>Recent Activity</h2>
            {dashboard.recentActivity && dashboard.recentActivity.length > 0 ? (
              <div>
                {dashboard.recentActivity.map((activity, i) => (
                  <div key={i} style={{ padding: '10px 0', borderBottom: '1px solid #f0f0f0' }}>
                    <span style={{ color: '#666', marginRight: '10px' }}>
                      {new Date(activity.timestamp).toLocaleString()}
                    </span>
                    <strong>{activity.action}</strong>: {activity.details}
                  </div>
                ))}
              </div>
            ) : (
              <p style={{ color: '#666' }}>No recent activity</p>
            )}
          </div>
        </>
      )}

      {/* Users Tab */}
      {activeTab === 'users' && (
        <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
            <h2 style={{ margin: 0 }}>User Management</h2>
            <select
              value={userRoleFilter}
              onChange={(e) => { setUserRoleFilter(e.target.value); setUsersPage(1); }}
              style={{ padding: '8px 15px', borderRadius: '5px', border: '1px solid #ddd' }}
            >
              <option value="">All Roles</option>
              <option value="EC">End Consumer</option>
              <option value="BC">Business Consumer</option>
              <option value="DP">Delivery Partner</option>
              <option value="DPCM">DPCM</option>
              <option value="SA">Super Admin</option>
            </select>
          </div>

          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid #e0e0e0' }}>
                <th style={{ padding: '12px', textAlign: 'left' }}>User</th>
                <th style={{ padding: '12px', textAlign: 'left' }}>Role</th>
                <th style={{ padding: '12px', textAlign: 'left' }}>Status</th>
                <th style={{ padding: '12px', textAlign: 'left' }}>Joined</th>
                <th style={{ padding: '12px', textAlign: 'center' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map(user => (
                <tr key={user.id} style={{ borderBottom: '1px solid #e0e0e0' }}>
                  <td style={{ padding: '12px' }}>
                    <div>{user.phone || user.email}</div>
                    <small style={{ color: '#999' }}>{user.id}</small>
                  </td>
                  <td style={{ padding: '12px' }}>
                    <span style={{
                      padding: '4px 8px',
                      borderRadius: '4px',
                      background: '#e3f2fd',
                      color: '#1976d2',
                      fontSize: '0.85em'
                    }}>
                      {user.role}
                    </span>
                  </td>
                  <td style={{ padding: '12px' }}>
                    <span style={{
                      padding: '4px 8px',
                      borderRadius: '4px',
                      background: user.isActive ? '#d4edda' : '#f8d7da',
                      color: user.isActive ? '#155724' : '#721c24',
                      fontSize: '0.85em'
                    }}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td style={{ padding: '12px', color: '#666' }}>
                    {new Date(user.createdAt).toLocaleDateString()}
                  </td>
                  <td style={{ padding: '12px', textAlign: 'center' }}>
                    <button
                      onClick={() => handleUserStatusChange(user.id, !user.isActive)}
                      style={{
                        padding: '6px 12px',
                        border: 'none',
                        borderRadius: '4px',
                        background: user.isActive ? '#dc3545' : '#28a745',
                        color: 'white',
                        cursor: 'pointer',
                        fontSize: '0.85em'
                      }}
                    >
                      {user.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <Pagination page={usersPage} totalPages={usersTotalPages} setPage={setUsersPage} />
        </div>
      )}

      {/* KYC Tab */}
      {activeTab === 'kyc' && (
        <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
            <h2 style={{ margin: 0 }}>KYC Requests</h2>
            <select
              value={kycStatusFilter}
              onChange={(e) => { setKYCStatusFilter(e.target.value); setKYCPage(1); }}
              style={{ padding: '8px 15px', borderRadius: '5px', border: '1px solid #ddd' }}
            >
              <option value="">All Status</option>
              <option value="PENDING">Pending</option>
              <option value="APPROVED">Approved</option>
              <option value="REJECTED">Rejected</option>
            </select>
          </div>

          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid #e0e0e0' }}>
                <th style={{ padding: '12px', textAlign: 'left' }}>Request ID</th>
                <th style={{ padding: '12px', textAlign: 'left' }}>User</th>
                <th style={{ padding: '12px', textAlign: 'left' }}>Type</th>
                <th style={{ padding: '12px', textAlign: 'left' }}>Status</th>
                <th style={{ padding: '12px', textAlign: 'left' }}>Submitted</th>
                <th style={{ padding: '12px', textAlign: 'center' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {kycRequests.map(kyc => (
                <tr key={kyc.id} style={{ borderBottom: '1px solid #e0e0e0' }}>
                  <td style={{ padding: '12px' }}>
                    <code style={{ fontSize: '0.85em' }}>{kyc.id.substring(0, 8)}...</code>
                  </td>
                  <td style={{ padding: '12px' }}>{kyc.userName || kyc.userId}</td>
                  <td style={{ padding: '12px' }}>{kyc.documentType || 'KYC'}</td>
                  <td style={{ padding: '12px' }}>
                    <span style={{
                      padding: '4px 8px',
                      borderRadius: '4px',
                      background: kyc.status === 'APPROVED' ? '#d4edda' : kyc.status === 'REJECTED' ? '#f8d7da' : '#fff3cd',
                      color: kyc.status === 'APPROVED' ? '#155724' : kyc.status === 'REJECTED' ? '#721c24' : '#856404',
                      fontSize: '0.85em'
                    }}>
                      {kyc.status}
                    </span>
                  </td>
                  <td style={{ padding: '12px', color: '#666' }}>
                    {new Date(kyc.createdAt).toLocaleDateString()}
                  </td>
                  <td style={{ padding: '12px', textAlign: 'center' }}>
                    {kyc.status === 'PENDING' && (
                      <>
                        <button
                          onClick={() => handleKYCApprove(kyc.id)}
                          style={{
                            padding: '6px 12px',
                            border: 'none',
                            borderRadius: '4px',
                            background: '#28a745',
                            color: 'white',
                            cursor: 'pointer',
                            marginRight: '5px',
                            fontSize: '0.85em'
                          }}
                        >
                          Approve
                        </button>
                        <button
                          onClick={() => handleKYCReject(kyc.id)}
                          style={{
                            padding: '6px 12px',
                            border: 'none',
                            borderRadius: '4px',
                            background: '#dc3545',
                            color: 'white',
                            cursor: 'pointer',
                            fontSize: '0.85em'
                          }}
                        >
                          Reject
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <Pagination page={kycPage} totalPages={kycTotalPages} setPage={setKYCPage} />
        </div>
      )}

      {/* Audit Logs Tab */}
      {activeTab === 'audit' && (
        <div style={{ background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
          <h2 style={{ marginTop: 0 }}>Audit Logs</h2>

          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid #e0e0e0' }}>
                <th style={{ padding: '12px', textAlign: 'left' }}>Timestamp</th>
                <th style={{ padding: '12px', textAlign: 'left' }}>User</th>
                <th style={{ padding: '12px', textAlign: 'left' }}>Action</th>
                <th style={{ padding: '12px', textAlign: 'left' }}>Entity</th>
                <th style={{ padding: '12px', textAlign: 'left' }}>IP Address</th>
              </tr>
            </thead>
            <tbody>
              {auditLogs.map(log => (
                <tr key={log.id} style={{ borderBottom: '1px solid #e0e0e0' }}>
                  <td style={{ padding: '12px', color: '#666', fontSize: '0.9em' }}>
                    {new Date(log.createdAt).toLocaleString()}
                  </td>
                  <td style={{ padding: '12px' }}>{log.userName || log.userId}</td>
                  <td style={{ padding: '12px' }}>
                    <span style={{
                      padding: '4px 8px',
                      borderRadius: '4px',
                      background: '#e3f2fd',
                      color: '#1976d2',
                      fontSize: '0.85em'
                    }}>
                      {log.action}
                    </span>
                  </td>
                  <td style={{ padding: '12px' }}>
                    {log.entityType} {log.entityId && <code style={{ fontSize: '0.8em' }}>({log.entityId.substring(0, 8)}...)</code>}
                  </td>
                  <td style={{ padding: '12px', color: '#666', fontFamily: 'monospace' }}>
                    {log.ipAddress || '-'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <Pagination page={logsPage} totalPages={logsTotalPages} setPage={setLogsPage} />
        </div>
      )}
    </div>
  );
};

// Helper Components
const StatCard = ({ title, value, color }) => (
  <div style={{
    background: 'white',
    padding: '20px',
    borderRadius: '10px',
    boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
    borderLeft: `4px solid ${color}`
  }}>
    <h3 style={{ margin: '0 0 10px', color: '#666', fontSize: '0.9em' }}>{title}</h3>
    <div style={{ fontSize: '2em', fontWeight: 'bold', color }}>{value.toLocaleString()}</div>
  </div>
);

const Pagination = ({ page, totalPages, setPage }) => (
  <div style={{ display: 'flex', justifyContent: 'center', gap: '10px', marginTop: '20px' }}>
    <button
      onClick={() => setPage(p => Math.max(1, p - 1))}
      disabled={page === 1}
      style={{
        padding: '8px 16px',
        border: '1px solid #ddd',
        borderRadius: '5px',
        background: page === 1 ? '#f5f5f5' : 'white',
        cursor: page === 1 ? 'not-allowed' : 'pointer'
      }}
    >
      Previous
    </button>
    <span style={{ padding: '8px 16px' }}>Page {page} of {totalPages}</span>
    <button
      onClick={() => setPage(p => Math.min(totalPages, p + 1))}
      disabled={page === totalPages}
      style={{
        padding: '8px 16px',
        border: '1px solid #ddd',
        borderRadius: '5px',
        background: page === totalPages ? '#f5f5f5' : 'white',
        cursor: page === totalPages ? 'not-allowed' : 'pointer'
      }}
    >
      Next
    </button>
  </div>
);

export default AdminDashboardPage;
