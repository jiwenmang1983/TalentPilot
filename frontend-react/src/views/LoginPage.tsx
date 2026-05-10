import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { api } from '@/services/api';

export default function LoginPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleLogin = async () => {
    setError('');
    try {
      const res = await api.login(username, password);
      if (res.token) {
        localStorage.setItem('token', res.token);
        navigate('/dashboard', { replace: true });
      } else {
        setError(res.message || 'Login failed');
      }
    } catch {
      setError('Network error');
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle>TalentPilot</CardTitle>
          <CardDescription>AI-Powered Recruitment Platform</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Input
              placeholder="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
            />
            <Input
              type="password"
              placeholder="Password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </div>
          {error && <p className="text-sm text-red-500">{error}</p>}
          <Button className="w-full" onClick={handleLogin}>
            Sign In
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
