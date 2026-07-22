#!/bin/sh
# Runs the sync worker on a loop: one full sync pass, sleep, repeat.
# Forwards SIGTERM/SIGINT to whichever child (the sync process or the sleep)
# is currently running so `docker stop` triggers a prompt, graceful shutdown
# instead of waiting out the stop-timeout and getting SIGKILLed.

INTERVAL_SECONDS="${SYNC_INTERVAL_SECONDS:-3600}"
terminate=0
trap 'terminate=1; [ -n "$child" ] && kill -TERM "$child" 2>/dev/null' TERM INT

while [ "$terminate" -eq 0 ]; do
    dotnet oculusit.sync.dll &
    child=$!
    wait "$child"

    [ "$terminate" -eq 1 ] && break

    echo "Sync pass finished. Sleeping ${INTERVAL_SECONDS}s before the next run."
    sleep "$INTERVAL_SECONDS" &
    child=$!
    wait "$child"
done

echo "Entrypoint loop stopped."
