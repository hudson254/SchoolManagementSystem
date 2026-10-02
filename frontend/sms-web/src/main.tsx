import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './index.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

// PWA service worker registration.
//
// Registration is handled by the `useServiceWorker` hook mounted inside the
// authenticated Layout, because it also needs to surface an available UPDATE to
// the user (a worker that downloaded but never activates leaves an installed user
// stuck on a stale frontend build).
//
// It used to be registered here instead, and only in production. Two problems:
//   - registering in `main.tsx` discarded the registration, so the waiting-worker
//     state could never be observed and an update could never be applied;
//   - the production-only guard meant the PWA behaviour was untestable locally.
//
// Do not re-add registration here; add it to the Layout via the hook.
