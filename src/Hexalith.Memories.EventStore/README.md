# Hexalith.Memories.EventStore

Zero-code DAPR pub/sub subscription for Hexalith.Memories. Auto-discovers CloudEvents published to a configured topic and funnels event payloads through the existing `IngestionWorkflow` without developer-written mapping code.

See `docs/dev/eventstore-integration.md` in the Hexalith.Memories repository for setup, CloudEvents envelope requirements, tenant/case routing configuration, and end-to-end examples.
