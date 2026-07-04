import { createContext, useContext, useState, useCallback } from 'react';
import type { ReactNode } from 'react';

interface Toast {
  id: number;
  message: string;
  variant: 'success' | 'danger' | 'warning' | 'info';
}

interface ToastContextValue {
  showToast: (message: string, variant?: Toast['variant']) => void;
}

const ToastContext = createContext<ToastContextValue>({
  showToast: () => undefined,
});

export function useToast() {
  return useContext(ToastContext);
}

let nextId = 0;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const showToast = useCallback(
    (message: string, variant: Toast['variant'] = 'danger') => {
      const id = ++nextId;
      setToasts((prev) => [...prev, { id, message, variant }]);
      setTimeout(() => {
        setToasts((prev) => prev.filter((t) => t.id !== id));
      }, 4000);
    },
    []
  );

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div
        style={{
          position: 'fixed',
          bottom: '1rem',
          left: '1rem',
          zIndex: 9999,
          display: 'flex',
          flexDirection: 'column',
          gap: '0.5rem',
        }}
      >
        {toasts.map((t) => (
          <div
            key={t.id}
            className={`alert alert-${t.variant} alert-dismissible shadow mb-0`}
            role="alert"
            style={{ minWidth: '280px' }}
          >
            {t.message}
            <button
              type="button"
              className="btn-close"
              onClick={() =>
                setToasts((prev) => prev.filter((x) => x.id !== t.id))
              }
            />
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}
