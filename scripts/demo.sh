#!/usr/bin/env bash
# End-to-end demo through the API gateway: builds a partner tree, sends profit events, checks commissions
# under both schemas, runs payouts and checks the wallets, including a payout to a blocked wallet that is cancelled.
#
# Requires: a running stack (docker compose up -d), bash, curl. jq is used for pretty output if present.
# Usage:    ./scripts/demo.sh [gateway-url]          (default http://localhost:8080)
set -euo pipefail

GATEWAY="${1:-http://localhost:8080}"
RUN="$(date +%s)" # suffix: the demo can be re-run against the same databases

ROOT="root-$RUN"; A="a-$RUN"; B="b-$RUN"; C="c-$RUN"; D="d-$RUN"

step() { printf '\n\033[1;34m== %s\033[0m\n' "$*"; }
note() { printf '\033[0;33m   %s\033[0m\n' "$*"; }
pretty() { if command -v jq >/dev/null 2>&1; then jq .; else cat; echo; fi; }

call() {
  local method=$1 path=$2 body=${3:-}
  printf '\033[0;36m-> %s %s %s\033[0m\n' "$method" "$path" "$body"
  if [[ -n $body ]]; then
    curl -sS -X "$method" "$GATEWAY$path" -H 'Content-Type: application/json' -d "$body" | pretty
  else
    curl -sS -X "$method" "$GATEWAY$path" | pretty
  fi
}

# Polls GET $1 until its body matches the regex $2 (commissions and payouts are processed asynchronously).
wait_for() {
  local path=$1 pattern=$2
  for _ in $(seq 1 60); do
    if curl -sS "$GATEWAY$path" | grep -Eq "$pattern"; then return 0; fi
    sleep 1
  done
  echo "Timed out waiting for $path to match $pattern" >&2
  return 1
}

# Starts a payout run and waits until the given partners have nothing accrued or reserved any more.
# The periodic run (every 30 s in docker-compose) may pick the commissions up first; that is fine too.
run_payouts_and_wait_paid() {
  call POST /api/payouts/run
  for partner in "$@"; do
    # Lists are JSON arrays: an empty array means nothing is left.
    wait_for "/api/commissions?partnerId=$partner&status=Accrued" '^\[\]$'
    wait_for "/api/commissions?partnerId=$partner&status=Reserved" '^\[\]$'
  done
}

step "Health"
curl -sS -o /dev/null -w "gateway /health/ready -> %{http_code}\n" "$GATEWAY/health/ready"

step "Start with the linear schema"
call PUT /api/admin/commission-schema '{"schemaType":"Linear"}'

step "Partner tree: $ROOT <- $A <- $B <- $C <- $D  (X <- Y: X invited Y)"
call POST /api/users "{\"externalId\":\"$ROOT\"}"
call POST /api/users "{\"externalId\":\"$A\",\"partnerId\":\"$ROOT\"}"
call POST /api/users "{\"externalId\":\"$B\",\"partnerId\":\"$A\"}"
call POST /api/users "{\"externalId\":\"$C\",\"partnerId\":\"$B\"}"
call POST /api/users "{\"externalId\":\"$D\",\"partnerId\":\"$C\"}"

step "Branch up of $D (level 1 = direct partner)"
call GET "/api/users/$D/upline"
step "Branch down of $ROOT"
call GET "/api/users/$ROOT/downline?depth=10"
step "A link that would close a cycle is rejected (422)"
call PUT "/api/users/$ROOT/partner" "{\"partnerId\":\"$D\"}"

step "Event e1-$RUN: $D earned 1000 (linear schema)"
note "expected: $C (L1) = 10, $B (L2) = 20, $A (L3) = 30, $ROOT (L4) = 40"
call POST /api/events "{\"externalId\":\"e1-$RUN\",\"userId\":\"$D\",\"profit\":1000}"
note "the same request again is idempotent (200 + the stored event)"
call POST /api/events "{\"externalId\":\"e1-$RUN\",\"userId\":\"$D\",\"profit\":1000}"
wait_for "/api/events/e1-$RUN" '"status":"Processed"'
call GET "/api/events/e1-$RUN"

step "Event e2-$RUN: $D lost 500 -> no commissions"
call POST /api/events "{\"externalId\":\"e2-$RUN\",\"userId\":\"$D\",\"profit\":-500}"
wait_for "/api/events/e2-$RUN" '"status":"Processed"'
call GET "/api/events/e2-$RUN"

step "Switch to the Fibonacci schema (only new calculations are affected)"
call PUT /api/admin/commission-schema '{"schemaType":"Fibonacci"}'

step "Event e3-$RUN: $D earned 1000 (Fibonacci schema)"
note "expected: $C = 10 (F1=1), $B = 10 (F2=1), $A = 20 (F3=2), $ROOT = 30 (F4=3); e1 keeps schemaType Linear"
call POST /api/events "{\"externalId\":\"e3-$RUN\",\"userId\":\"$D\",\"profit\":1000}"
wait_for "/api/events/e3-$RUN" '"status":"Processed"'
call GET "/api/events/e3-$RUN"

step "Events of $D (list without commission details)"
call GET "/api/events?userId=$D"

step "Payout run (orchestrated saga per partner)"
run_payouts_and_wait_paid "$C" "$B" "$A" "$ROOT"
call GET "/api/payouts?partnerId=$ROOT"

step "Wallet balances = sums of paid commissions"
note "expected: $C = 20, $B = 30, $A = 50, $ROOT = 70, $D = 0"
for user in "$C" "$B" "$A" "$ROOT" "$D"; do call GET "/api/wallets/$user"; done

step "Payout history of $C: which commissions were paid and when"
call GET "/api/wallets/$C/payouts"

step "Event details now show the commissions as paid"
call GET "/api/events/e1-$RUN"

step "Compensation: block the wallet of $A, accrue e4-$RUN (100, Fibonacci), run a payout"
call POST "/api/admin/wallets/$A/block"
call POST /api/events "{\"externalId\":\"e4-$RUN\",\"userId\":\"$D\",\"profit\":100}"
wait_for "/api/events/e4-$RUN" '"status":"Processed"'
run_payouts_and_wait_paid "$C" "$B" "$ROOT"
wait_for "/api/payouts?partnerId=$A&state=Compensated" '"payoutId"'
note "the payout of $A is Compensated: Reserving -> Crediting -> Compensating -> Compensated"
PAYOUT_A="$(curl -sS "$GATEWAY/api/payouts?partnerId=$A&state=Compensated" | grep -oE '"payoutId":"[^"]+"' | head -1 | cut -d'"' -f4)"
call GET "/api/payouts/$PAYOUT_A"
note "its commission is accrued again, the balance of $A is unchanged (50)"
call GET "/api/commissions?partnerId=$A&status=Accrued"
call GET "/api/wallets/$A"

step "Unblock the wallet and pay again"
call POST "/api/admin/wallets/$A/unblock"
run_payouts_and_wait_paid "$A"
note "expected balances: $C = 21, $B = 31, $A = 52, $ROOT = 73"
for user in "$C" "$B" "$A" "$ROOT"; do call GET "/api/wallets/$user"; done

step "Restore the linear schema"
call PUT /api/admin/commission-schema '{"schemaType":"Linear"}'

printf '\n\033[1;32mDone.\033[0m Telemetry (Aspire Dashboard): http://localhost:18888  Kafka UI: http://localhost:8088  pgAdmin: http://localhost:5050\n'
