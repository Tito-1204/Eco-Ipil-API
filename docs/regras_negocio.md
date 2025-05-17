# Regras de Negócio - Sistema EcoIpil

## 1. Gestão de Usuários

### 1.1 Cadastro de Usuários
- O sistema deve validar os seguintes campos obrigatórios:
  - Nome completo (mínimo 3 caracteres)
  - Email (formato válido e único no sistema)
  - Senha (mínimo 8 caracteres, incluindo números e letras)
  - Telefone (formato válido)
  - Data de nascimento (maior de 18 anos)
  - Localização (coordenadas geográficas válidas)

### 1.2 Autenticação
- O sistema deve implementar autenticação via JWT (JSON Web Token)
- Tokens devem expirar após 24 horas
- Usuários devem ser deslogados após 30 minutos de inatividade
- Máximo de 3 tentativas de login antes de bloqueio temporário
- Recuperação de senha via email com token único de 1 hora

### 1.3 Perfil do Usuário
- Usuários podem atualizar:
  - Foto de perfil (formatos: JPG, PNG, máximo 5MB)
  - Telefone
  - Localização
  - Gênero (opcional)
- Histórico de alterações deve ser mantido
- Status do usuário pode ser:
  - Ativo
  - Inativo
  - Bloqueado
  - Suspenso

## 2. Carteira Digital

### 2.1 Pontuação
- Pontos são acumulados através de:
  - Reciclagem de materiais (baseado no peso e tipo)
  - Participação em campanhas
  - Conquistas
- Pontos não podem ser negativos
- Histórico de pontuação deve ser mantido
- Sistema de níveis baseado em pontos acumulados:
  - Nível 1: 0-1000 pontos
  - Nível 2: 1001-5000 pontos
  - Nível 3: 5001-10000 pontos
  - Nível 4: 10001-20000 pontos
  - Nível 5: 20001+ pontos

### 2.2 Saldo
- Saldo é calculado em reais (AOA)
- Taxa de conversão: 100 pontos = 1 AOA
- Saldo mínimo: 0 AOA
- Transações devem ser registradas com:
  - Data e hora
  - Tipo (crédito/débito)
  - Valor
  - Origem/destino
  - Status

## 3. Sistema de Reciclagem

### 3.1 Registro de Reciclagem
- Validações obrigatórias:
  - Peso mínimo: 0.1 kg
  - Material válido e aceito pelo ecoponto
  - Ecoponto ativo e com capacidade
  - Agente autorizado
- Pontuação por material:
  - Plástico: 10 pontos/kg
  - Papel: 5 pontos/kg
  - Metal: 15 pontos/kg
  - Vidro: 8 pontos/kg
  - Eletrônicos: 20 pontos/kg

### 3.2 Ecopontos
- Requisitos de cadastro:
  - Nome único
  - Localização precisa
  - Capacidade total
  - Materiais aceitos
  - Horário de funcionamento
- Status do ecoponto:
  - Ativo
  - Inativo
  - Manutenção
  - Lotado
- Monitoramento:
  - Nível de ocupação
  - Temperatura
  - Status dos sensores
  - Alertas de capacidade

## 4. Campanhas

### 4.1 Criação de Campanhas
- Campos obrigatórios:
  - Título único
  - Descrição detalhada
  - Data de início e fim
  - Pontos oferecidos
  - Regras de participação
- Validações:
  - Data de início > data atual
  - Data de fim > data de início
  - Pontos > 0
  - Sem sobreposição de datas

### 4.2 Participação em Campanhas
- Requisitos:
  - Usuário ativo
  - Campanha ativa
  - Dentro do período válido
  - Não participante anteriormente
- Registro de participação:
  - Data e hora
  - Status
  - Pontos ganhos
  - Atividades realizadas

## 5. Recompensas

### 5.1 Cadastro de Recompensas
- Informações obrigatórias:
  - Nome único
  - Descrição detalhada
  - Tipo (produto/serviço/desconto)
  - Pontos necessários
  - Quantidade disponível
- Validações:
  - Pontos > 0
  - Quantidade >= 0
  - Preço de mercado (se aplicável)

### 5.2 Resgate de Recompensas
- Requisitos:
  - Usuário com pontos suficientes
  - Recompensa disponível
  - Não resgatada anteriormente
- Processo:
  - Verificação de elegibilidade
  - Dedução de pontos
  - Atualização de estoque
  - Geração de código/ticket

## 6. Conquistas

### 6.1 Sistema de Conquistas
- Tipos de conquistas:
  - Reciclagem (quantidade/tipo)
  - Participação (campanhas)
  - Tempo (dias consecutivos)
  - Nível (pontos totais)
- Validações:
  - Critérios únicos
  - Pontos > 0
  - Não repetível

### 6.2 Desbloqueio de Conquistas
- Processo automático:
  - Verificação diária
  - Notificação ao usuário
  - Registro de data
  - Atribuição de pontos

## 7. Segurança e Auditoria

### 7.1 Proteção de Dados
- Criptografia:
  - Senhas (bcrypt)
  - Dados sensíveis
  - Tokens
- Validações:
  - Input sanitization
  - Proteção contra SQL injection
  - Rate limiting

### 7.2 Logs e Auditoria
- Registro de:
  - Operações críticas
  - Alterações de dados
  - Tentativas de acesso
  - Erros do sistema
- Retenção:
  - Logs de erro: 1 ano
  - Logs de auditoria: 5 anos
  - Logs de acesso: 6 meses

## 8. Performance e Escalabilidade

### 8.1 Otimização
- Cache:
  - Dados frequentemente acessados
  - Resultados de consultas complexas
  - Configurações do sistema
- Índices:
  - Chaves primárias
  - Chaves estrangeiras
  - Campos de busca frequente

### 8.2 Monitoramento
- Métricas:
  - Tempo de resposta
  - Uso de recursos
  - Erros por minuto
  - Usuários ativos
- Alertas:
  - Alta latência
  - Erros críticos
  - Capacidade do banco
  - Serviços indisponíveis 