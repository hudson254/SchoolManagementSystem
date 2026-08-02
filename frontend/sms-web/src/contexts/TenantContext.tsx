import React, { createContext, useContext, useState, useEffect } from 'react';
import { apiClient } from '../services/api';

interface Tenant {
  id: string;
  name: string;
  subdomain: string;
  logoUrl?: string;
  themeColor?: string;
}

interface TenantContextType {
  tenant: Tenant | null;
  isLoading: boolean;
  setTenant: (tenant: Tenant) => void;
}

const TenantContext = createContext<TenantContextType | undefined>(undefined);

export const TenantProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [tenant, setTenant] = useState<Tenant | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadTenant = async () => {
      try {
        // Get tenant from subdomain or default
        const subdomain = window.location.hostname.split('.')[0];
        const response = await apiClient.get(`/tenants/${subdomain}`);
        setTenant(response.data);
      } catch (error) {
        console.error('Failed to load tenant:', error);
        // Use default tenant
        try {
          const response = await apiClient.get('/tenants/default');
          setTenant(response.data);
        } catch {
          // Fallback tenant
          setTenant({
            id: '11111111-1111-1111-1111-111111111111',
            name: 'Default School',
            subdomain: 'main',
          });
        }
      } finally {
        setIsLoading(false);
      }
    };

    loadTenant();
  }, []);

  return (
    <TenantContext.Provider value={{ tenant, isLoading, setTenant }}>
      {children}
    </TenantContext.Provider>
  );
};

export const useTenant = () => {
  const context = useContext(TenantContext);
  if (context === undefined) {
    throw new Error('useTenant must be used within a TenantProvider');
  }
  return context;
};