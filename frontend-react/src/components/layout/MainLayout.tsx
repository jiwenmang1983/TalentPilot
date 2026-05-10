import { Outlet, Link, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';

const navLinks = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/candidates', label: 'Candidates' },
  { to: '/interviews', label: 'Interviews' },
  { to: '/reports', label: 'Reports' },
  { to: '/settings', label: 'Settings' },
];

export default function MainLayout() {
  const navigate = useNavigate();

  const handleLogout = () => {
    localStorage.removeItem('token');
    navigate('/login');
  };

  return (
    <div className="flex min-h-screen">
      {/* Sidebar */}
      <aside className="w-56 border-r bg-slate-50 p-4">
        <h1 className="mb-6 text-xl font-bold">TalentPilot</h1>
        <nav className="space-y-1">
          {navLinks.map((link) => (
            <Link
              key={link.to}
              to={link.to}
              className="block rounded px-3 py-2 text-sm hover:bg-slate-200"
            >
              {link.label}
            </Link>
          ))}
        </nav>
      </aside>

      {/* Main content */}
      <div className="flex-1 flex flex-col">
        <header className="flex items-center justify-between border-b px-6 py-3">
          <span className="text-sm text-muted-foreground">Admin</span>
          <Button variant="outline" size="sm" onClick={handleLogout}>
            Logout
          </Button>
        </header>
        <main className="flex-1 p-6">
          <Card>
            <CardContent className="p-6">
              <Outlet />
            </CardContent>
          </Card>
        </main>
      </div>
    </div>
  );
}
