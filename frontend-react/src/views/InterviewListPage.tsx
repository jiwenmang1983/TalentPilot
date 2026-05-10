import { useEffect, useState } from 'react';
import { api } from '@/services/api';

export default function InterviewListPage() {
  const [data, setData] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getInterviews()
      .then((r) => { setData(Array.isArray(r) ? r : []); setLoading(false); })
      .catch(() => setLoading(false));
  }, []);

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Interviews</h2>
      {loading ? <p>Loading...</p> : data.length === 0 ? <p className="text-muted-foreground">No data</p> : (
        <ul>{data.map((i) => <li key={i.id}>{JSON.stringify(i)}</li>)}</ul>
      )}
    </div>
  );
}
