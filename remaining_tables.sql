SELECT t.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, c.IS_NULLABLE
FROM INFORMATION_SCHEMA.TABLES t
JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME
WHERE t.TABLE_TYPE = 'BASE TABLE'
AND t.TABLE_NAME IN ('Ratings', 'ReferralCodes', 'Referrals', 'RolePermissions', 'SavedAddresses',
'ServiceAreas', 'SettlementItems', 'Settlements', 'StateMasters', 'SubscriptionInvoices',
'SubscriptionPlans', 'SystemConfigs', 'Users', 'UserSessions', 'UserSubscriptions',
'VehicleLicenseVerifications', 'Wallets', 'WalletTransactions')
ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION
