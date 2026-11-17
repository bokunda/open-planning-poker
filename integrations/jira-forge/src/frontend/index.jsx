import React, { useCallback, useEffect, useState } from 'react';
import ForgeReconciler, { Button, Inline, Lozenge, Stack, Text } from '@forge/react';
import { invoke, router, view } from '@forge/bridge';

const APP = 'https://app.openplanningpoker.com';

/** Link that starts a game with this issue as the first ticket (the app's /estimate page, OPP-120). */
export function estimateUrl(siteUrl, issue) {
  const issueUrl = `${siteUrl.replace(/\/$/, '')}/browse/${issue.key}`;
  const params = new URLSearchParams({ key: issue.key, title: issue.summary, url: issueUrl });
  return `${APP}/estimate?${params.toString()}`;
}

/**
 * Issue panel "Estimate with Open Planning Poker": shows the current story points and opens a planning poker game
 * for this issue. The team votes in the app; the host saves the agreed estimate back with "Save to KEY".
 */
const App = () => {
  const [issue, setIssue] = useState(null);
  const [siteUrl, setSiteUrl] = useState('');
  const load = useCallback(() => { invoke('getIssue').then(setIssue); }, []);

  useEffect(() => {
    load();
    view.getContext().then(context => setSiteUrl(context.siteUrl ?? ''));
  }, [load]);

  if (!issue) return <Text>Loading…</Text>;
  return (
    <Stack space="space.150">
      <Inline space="space.100" alignBlock="center">
        <Text>Story points:</Text>
        {issue.storyPoints === null
          ? <Lozenge>Not estimated</Lozenge>
          : <Lozenge appearance="success">{String(issue.storyPoints)}</Lozenge>}
      </Inline>
      <Text>Everyone votes in secret, then the cards are turned over together. After the reveal, the host saves the estimate to {issue.key}.</Text>
      <Inline space="space.100">
        <Button appearance="primary" isDisabled={!siteUrl} onClick={() => router.open(estimateUrl(siteUrl, issue))}>
          Estimate in Open Planning Poker
        </Button>
        <Button appearance="subtle" onClick={load}>Refresh</Button>
      </Inline>
    </Stack>
  );
};

ForgeReconciler.render(<React.StrictMode><App /></React.StrictMode>);
