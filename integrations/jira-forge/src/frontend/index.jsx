import React, { useEffect, useState } from 'react';
import ForgeReconciler, { Button, Stack, Text } from '@forge/react';
import { invoke, router } from '@forge/bridge';

/**
 * Issue panel: shows the issue and opens Open Planning Poker to start a room. A full version would create the room
 * for this issue through an Open Planning Poker API and write the estimate back with requestJira.
 */
const App = () => {
  const [issue, setIssue] = useState(null);
  useEffect(() => { invoke('getIssue').then(setIssue); }, []);
  if (!issue) return <Text>Loading…</Text>;
  return (
    <Stack space="space.100">
      <Text>Estimate {issue.key} with your team: everyone votes in secret, then the cards are revealed together.</Text>
      <Button appearance="primary" onClick={() => router.open('https://app.openplanningpoker.com/game')}>
        Start planning poker
      </Button>
    </Stack>
  );
};

ForgeReconciler.render(<React.StrictMode><App /></React.StrictMode>);
