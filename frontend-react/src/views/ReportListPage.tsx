import { useEffect, useState } from 'react';
import { api } from '@/services/api';

export default function ReportListPage() {
  const [data, setData] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getReports()
      .then((r) => { setData(Array.isArray(r) ? r : []); setLoading(false); })
      .catch(() => setLoading(false));
  }, []);

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Reports</h2>
      {loading ? <p>Loading...</p> : data.length === 0 ? <p className="text-muted-foreground">No data</p> : (
        <ul>{data.map((r) => <li key={r.id}>{JSON.stringify(r)}</li>)}</ul>
      )}
    </div>
  );
}
