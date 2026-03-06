# Agent Architecture Patterns — Augmented Games Reference

Extracted from the Agent Academy curriculum (Recruit + Operative tracks).
Only logic patterns and architecture snippets. No fluff.

---

## Part 1 — Core Agent Platform Patterns

### 1. Orchestrator + Specialized Agents

The hub-and-spoke multi-agent architecture. Matches the swarm model directly.

```yaml
agent_system:
  orchestrator:
    responsibilities:
      - receive_requests
      - delegate_tasks
      - aggregate_results

  agents:
    intake_agent:
      responsibility: data_ingestion
    analysis_agent:
      responsibility: evaluate_data
    strategy_agent:
      responsibility: produce_decision
```

Maps to the augmented games swarm:

```
Swarm (orchestrator)
   |
AI agents (strategy, scouting, voting)
```

---

### 2. Child Agent vs Connected Agent

Policy decision for agent relationship type.

```yaml
agent_relationship_decision:

  child_agent:
    use_when:
      - same_deployment
      - same_team
      - shared_context
      - lightweight_specialization

  connected_agent:
    use_when:
      - independent_lifecycle
      - separate_authentication
      - reusable_across_systems
      - separate_deployment
```

```python
def choose_agent_type(scope, reuse):
    if scope == "internal" and reuse == False:
        return "child_agent"
    return "connected_agent"
```

---

### 3. Event Trigger — Autonomous Agent

Agents reacting to external signals without user interaction.

```yaml
event_trigger_system:

  trigger:
    source: external_system

  payload:
    - event_id
    - entity_id
    - metadata

  agent_logic:
    - evaluate_payload
    - choose_action
    - execute_action
```

```python
def handle_event(payload):
    action = planner.decide(payload)
    if action == "process":
        run_processing(payload)
    if action == "notify":
        send_notification(payload)
```

Applies to: QR checkpoint scans, race progress updates, draft events.

---

### 4. Data Grounding (Dynamic Knowledge)

AI decisions using live data — RAG-lite architecture.

```yaml
grounding:

  data_source:
    - database
    - api
    - knowledge_base

  retrieval:
    filter: request_context
    limit: N_records

  injection: context_prompt
```

Reasoning pipeline:

```
user request
   |
retrieve relevant records
   |
inject records into prompt
   |
AI reasoning
```

Applies to: athlete stats, discipline constraints, equipment rules, evaluation criteria.

---

### 5. Structured Prompt Processing Pipeline

Generic analysis agent pipeline.

```yaml
analysis_pipeline:

  step_1_extract:
    fields:
      - name
      - attributes
      - metadata

  step_2_evaluate:
    criteria:
      - must_have
      - nice_to_have

  step_3_score:
    metrics:
      - confidence
      - evidence

  step_4_generate_output:
    format: structured
```

```python
def pipeline(input):
    data = extract(input)
    evaluation = evaluate(data)
    score = score_result(evaluation)
    return build_output(score)
```

Applies to: Clawbot applicant evaluation and scoring.

---

### 6. Agent Instruction Guardrail System

Execution constraints for agents — policy enforcement.

```yaml
agent_instruction_rules:

  allowed_tools:
    - tool_a
    - tool_b

  forbidden_topics:
    - sensitive_data
    - private_info

  formatting_rules:
    response_format: structured
```

Applies to: anonymization guardrails (`hidden_data: name, photos`), `strategy_modification: allowed_after_lock: false`, `skipping_checkpoints: allowed: false`.

---

### 7. Multimodal Processing Pattern

Agents reading documents, images, structured data.

```yaml
multimodal_processor:

  inputs:
    - text
    - image
    - pdf

  processing:
    - extract_data
    - normalize_data

  output:
    format: json
```

```python
def analyze_document(file):
    text = vision_model.extract(file)
    structured = parse(text)
    return structured
```

Applies to: extracting structured athlete stats and experience from application documents before anonymization.

---

### 8. Model Selection Policy

Multiple LLMs routed by task complexity.

```yaml
model_policy:

  tasks:
    simple:
      model: fast_model
    mixed:
      model: auto_model
    complex:
      model: reasoning_model
```

```python
def select_model(task_complexity):
    if task_complexity == "high":
        return "reasoning_model"
    if task_complexity == "medium":
        return "auto_model"
    return "fast_model"
```

Applies to: complex deliberation (reasoning model) vs real-time checkpoint monitoring (fast model).

---

### 9. Agent Base Schema

Fundamental four-component agent definition.

```yaml
agent:

  knowledge:
    - documents
    - databases

  tools:
    - actions
    - integrations

  topics:
    - workflows
    - conversation_paths

  instructions:
    - behavior_rules
```

Use this as the base schema for every Clawbot.

---

### 10. MCP External Tool Integration

Universal tool connector — USB-C for AI tools.

```yaml
tool_integration:

  protocol: MCP

  tools:
    - email
    - calendar
    - database
    - file_storage

  access_control:
    - authentication
    - permissions
```

```python
def call_tool(tool, args):
    server = mcp.connect(tool)
    return server.execute(args)
```

Applies to: QR checkpoint scanner API, race tracking feed, team progress board.

---

## Part 2 — Enterprise Agent Runtime Patterns

### 11. Computer-Using Agent (CUA)

Agents controlling real software — the physical executor layer.

```yaml
computer_using_agent:

  environment:
    type:
      - desktop
      - browser
      - virtual_machine

  interface:
    actions:
      - mouse_click
      - screen_read

  security:
    credentials_store: secure_vault

  control:
    human_assistance: optional
```

```python
def run_cua_task(task):
    while not task.complete:
        screen = capture_screen()
        action = agent.plan(screen)
        execute(action)
```

Usage pattern:

```
AI strategy -> CUA -> operate software
```

---

### 12. Code Interpreter Agent

LLM + deterministic computation layer. Critical for AI reliability.

```yaml
code_interpreter:

  execution_environment:
    runtime: python
    sandbox: isolated

  capabilities:
    - data_analysis
    - statistics
    - file_generation
    - chart_generation

  inputs:
    - csv
    - excel
    - structured_data
```

```python
def analyze_data(file):
    df = load(file)
    result = compute(df)
    chart = visualize(df)
    return result, chart
```

Key concept:

```
LLM reasoning + deterministic compute
```

---

### 13. Knowledge Retrieval Architecture

Enterprise RAG — external knowledge ingestion and reasoning.

```yaml
knowledge_system:

  sources:
    - websites
    - sharepoint
    - onedrive
    - search_engine

  retrieval:
    - query
    - filter
    - rank

  reasoning: llm_analysis
```

```python
def retrieve_knowledge(query):
    docs = search(query)
    ranked = rank(docs)
    return ranked[:k]
```

---

### 14. Governance and Security Model

```yaml
security:

  authentication:
    methods:
      - sso
      - api_key
      - oauth

  access_control:
    roles:
      - editor
      - viewer

  protection:
    data_loss_prevention: true
```

```python
def authorize(user, action):
    if not user.authenticated:
        raise AccessDenied
    if action not in user.permissions:
        raise Forbidden
```

---

### 15. Voice-Enabled Agent

Speech-based AI interaction layer.

```yaml
voice_agent:

  input:
    - speech
    - dtmf

  processing:
    - speech_to_text
    - intent_detection

  output:
    - text_to_speech
```

```python
def handle_call(audio):
    text = speech_to_text(audio)
    intent = classify(text)
    response = agent.respond(intent)
    return text_to_speech(response)
```

---

### 16. Human-in-the-Loop Workflow

Decision flows requiring human review.

```yaml
human_in_loop:

  step_1: ai_proposal
  step_2: human_review
  step_3: approval_or_rejection
```

```python
def decision_flow(data):
    proposal = ai.evaluate(data)
    if proposal.requires_review:
        return request_human(proposal)
    return execute(proposal)
```

Applies to: operator override during race, clawbot_operator role.

---

### 17. Agent Observability and Monitoring

Production debugging and runtime metrics.

```yaml
observability:

  telemetry:
    - events
    - logs
    - metrics

  dashboards:
    - analytics
    - kpi_tracking

  evaluation: automated_tests
```

```python
metrics = {
    "sessions": count,
    "success_rate": percentage,
    "latency": ms
}
```

Applies to: real-time checkpoint scan tracking, team progress monitoring.

---

### 18. Deployment Pipeline for Agents

Enterprise lifecycle management.

```yaml
deployment:

  environments:
    - dev
    - test
    - production

  packaging: solution_bundle

  pipeline:
    - build
    - test
    - deploy
```

---

### 19. Document Generation

Producing structured output artifacts from prompts.

```yaml
document_generation:

  template: word_template

  inputs:
    - entity_data
    - criteria_data
    - prompt_instructions

  output:
    format: docx
```

Applies to: Clawbot strategy output — athlete assignments, discipline, equipment, route, pacing plan — locked before race start.

---

### 20. Bring-Your-Own-Model Routing

Custom models integrated alongside base models.

```yaml
model_integration:

  models:
    - base_model
    - custom_model

  selection: routing_policy

  capabilities:
    - reasoning
    - search
    - prediction
```

```python
def route_task(task):
    if task.requires_custom_model:
        return custom_model.run(task)
    return base_model.run(task)
```

---

## Combined Architecture

Full AI agent platform blueprint derived from all patterns above.

```
                    +---------------------------+
                    |        Orchestrator       |
                    +-------------+-------------+
                                  |
       +------------------+-------+-------+------------------+
       |                  |               |                  |
   Knowledge           Tools           Agents            Interfaces
       |                  |               |                  |
      RAG               MCP            Swarms          Voice / CUA
       |                  |               |                  |
       +------------------+-------+-------+------------------+
                                  |
                         Compute Layer
                     (Code Interpreter / Grounding)
                                  |
                       Governance + Security
                                  |
                       Observability + ALM
```

---

## Part 3 — Module Recommendations for Augmented Games

Which Agent Academy modules to study, and why they apply to the swarm system.

### Recruit Track

#### Directly Relevant

| Module | Why |
|--------|-----|
| 02 — Copilot Studio Fundamentals | Teaches the four pillars (Knowledge, Tools, Topics, Instructions) — exactly what each Clawbot needs: athlete data as knowledge, voting/draft actions as tools, instructions governing evaluation rules |
| 06 — Create Agent from Conversation | Building a custom agent grounded on your own data — maps directly to grounding each Clawbot on athlete profiles, race rules, discipline constraints |
| 07 — Topics with Triggers | Intent routing + variables — needed for the evaluation flow: trigger on "new applicant submitted", gather data, store candidate scores across deliberation steps |
| 09 — Agent Flows | The automation backbone — capturing draft picks, locking the strategy (`strategy_locked_before_race: true`), sending notifications to athletes and operators, writing results back to a data store |
| 10 — Event Triggers | Autonomous operation — real-time checkpoint scan events trigger agent responses without human prompting; also powers the swarm's race monitoring (`checkpoint_scans`, `team_progress`) |

#### Supporting

| Module | Why |
|--------|-----|
| 08 — Adaptive Cards | Clawbot operator UI — displaying anonymized applicant cards (`visible_data: stats, experience, personality`) and strategy output in a structured, interactive format |
| 04 — Creating a Solution | Packaging all four swarm agents + flows into a deployable Power Platform solution for the event |

#### Background / Prerequisite

| Module | Why |
|--------|-----|
| 01 — Introduction to Agents | Conceptual grounding before building swarm agents |
| 00 — Course Setup | Environment prerequisite |

#### Less Relevant

| Module | Why |
|--------|-----|
| 03 — Declarative Agent for M365 | M365 Copilot extension model — only useful if Clawbots need to operate inside Teams; doesn't fit the swarm/draft/race model |
| 05 — Pre-built Agents | Microsoft templates, not applicable to custom game logic |

---

### Operative Track

#### Must-Have (core architecture)

| Module | Why |
|--------|-----|
| O-03 — Multi-Agent Systems | The swarm IS a multi-agent system. Child agents vs connected agents maps directly to Clawbot architecture: one orchestrator per swarm + specialists for evaluation, voting, draft execution. Communication patterns between agents = deliberation. |
| O-01 — Get Started (Hiring Agent) | Structurally almost identical to the Clawbot use case: import applicants, AI evaluates candidates, build an orchestrator. The Dataverse schema and solution setup are directly reusable. |
| O-04 — Automate Triggers | QR checkpoint scans are external events. Agents need to detect them autonomously, update `team_progress`, and act without a human triggering them. |
| O-08 — Dataverse Grounding | Athlete stats, discipline constraints, equipment rules, evaluation criteria all live in Dataverse. Prompts need live access to this data so swarms make current decisions — not static ones. |

#### High Value (key capabilities)

| Module | Why |
|--------|-----|
| O-02 — Agent Instructions | Each Clawbot needs precise instructions: how to weight evaluation criteria, how to vote, what it's forbidden to do (`strategy_modification: allowed_after_lock: false`). Bad instructions = bad strategy. |
| O-09 — Document Generation | The strategy output is a structured artifact: athlete assignments, discipline, equipment, routes, pacing plan. Document generation from a prompt is exactly the right mechanism for `strategy_locked_before_race`. |
| O-10 — MCP | The race checkpoint QR scanner, real-time tracking feed, and team progress board are external systems. MCP is the standard way to connect Clawbots to them without custom connectors. |
| O-06 — AI Safety | `hidden_data: name, photos` (anonymization guardrails), prohibited actions (no checkpoint skipping), and content moderation to keep agent deliberation professional and unbiased. |

#### Useful (supporting)

| Module | Why |
|--------|-----|
| O-07 — Multimodal Prompts | If athlete applications include PDFs or photos, extracting structured stats/experience as JSON before anonymization is a multimodal prompt use case. |
| O-05 — Model Selection | Tune the deliberation model for complex reasoning (draft strategy) vs speed (checkpoint monitoring). |

---

### Combined Study Order

```
O-03 -> O-01 -> O-02 -> O-04 -> O-08
-> R-06 -> R-07 -> R-09 -> R-10
-> O-09 -> O-10 -> O-06
-> O-07 -> O-05 -> R-08 -> R-04
```

The operative track is significantly more relevant than the recruit track for this use case — the swarm architecture and autonomous race monitoring are firmly operative-level concerns.

---

## Priority Extraction for Augmented Games

The six patterns with the highest leverage for the swarm system:

| # | Pattern | Why |
|---|---------|-----|
| 1 | Orchestrator + specialist agents | Swarm architecture |
| 2 | Event-driven triggers | Checkpoint scans, race events |
| 3 | Dynamic data grounding (RAG) | Live athlete and race data |
| 4 | Structured reasoning pipelines | Evaluation and scoring |
| 5 | Agent guardrail policies | Anonymization, strategy lock |
| 6 | Model selection policy | Deliberation vs monitoring |
