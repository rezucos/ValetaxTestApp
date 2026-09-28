#!/bin/sh
# Creates the Kafka topics of the system (auto-creation is disabled on the broker).
# Every consumed topic gets a "<topic>.dlt" dead-letter companion.
set -eu

BOOTSTRAP_SERVERS="${BOOTSTRAP_SERVERS:-kafka:9092}"
PARTITIONS="${PARTITIONS:-6}"
REPLICATION_FACTOR="${REPLICATION_FACTOR:-1}"
KAFKA_TOPICS=/opt/kafka/bin/kafka-topics.sh

create_topic() {
  "$KAFKA_TOPICS" --bootstrap-server "$BOOTSTRAP_SERVERS" --create --if-not-exists \
    --topic "$1" --partitions "$2" --replication-factor "$REPLICATION_FACTOR"
}

for topic in \
  partners.user-registered.v1 \
  events.profit-event-registered.v1 \
  commissions.commissions-calculated.v1 \
  commissions.commissions-paid.v1 \
  payouts.commission-commands.v1 \
  payouts.wallet-commands.v1 \
  payouts.saga-replies.v1
do
  create_topic "$topic" "$PARTITIONS"
  create_topic "$topic.dlt" 1
done

"$KAFKA_TOPICS" --bootstrap-server "$BOOTSTRAP_SERVERS" --list
